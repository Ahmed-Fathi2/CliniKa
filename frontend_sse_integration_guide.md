# 🔔 Frontend Integration Guide — SSE Diagnosis Notification

## Overview

After the frontend triggers a diagnosis (`POST /api/Sessions/{id}/diagnose`), it needs to **know when LangFlow finishes processing**. Instead of polling, the backend now provides a **Server-Sent Events (SSE)** endpoint that pushes a real-time notification.

---

## The Flow (Step by Step)

```
1. User clicks "Diagnose" → Frontend sends POST /api/Sessions/{id}/diagnose
2. Backend responds 200 OK immediately ("Processing started")
3. Frontend IMMEDIATELY opens SSE connection to GET /api/Sessions/{id}/diagnosis-stream
4. Connection stays open... (loading spinner shown to user)
5. LangFlow finishes → calls POST /api/{id}/callback
6. Backend saves result → pushes SSE event "diagnosis-complete"
7. Frontend receives event → closes SSE → fetches full data via GET request
```

---

## SSE Endpoint Details

| Property | Value |
|---|---|
| **URL** | `GET /api/Sessions/{sessionId}/diagnosis-stream` |
| **Method** | GET |
| **Content-Type** | `text/event-stream` |
| **Auth** | None required (can be added later) |

### Events Emitted

| Event Name | When | Data (JSON) |
|---|---|---|
| `connected` | Immediately after connection opens | `{"sessionId": 123}` |
| `diagnosis-complete` | When LangFlow callback is received | `{"sessionId": 123, "status": "completed"}` |

---

## JavaScript Implementation (Vanilla JS)

```javascript
function waitForDiagnosis(sessionId) {
  return new Promise((resolve, reject) => {
    // 1. Create EventSource connection to the SSE endpoint
    const eventSource = new EventSource(
      `https://your-api-domain/api/Sessions/${sessionId}/diagnosis-stream`
    );

    // 2. Listen for the "connected" event (confirms stream is alive)
    eventSource.addEventListener('connected', (event) => {
      const data = JSON.parse(event.data);
      console.log(`✅ SSE connected for session ${data.sessionId}`);
    });

    // 3. Listen for the "diagnosis-complete" event
    eventSource.addEventListener('diagnosis-complete', (event) => {
      const data = JSON.parse(event.data);
      console.log(`🎉 Diagnosis complete for session ${data.sessionId}`);

      // 4. Close the SSE connection (we're done!)
      eventSource.close();

      // 5. Resolve the promise — caller can now fetch the full data
      resolve(data.sessionId);
    });

    // 6. Handle errors (network issues, server closed connection, etc.)
    eventSource.onerror = (error) => {
      // When the server closes the connection after sending the event,
      // EventSource fires an error with readyState = CLOSED.
      // This is EXPECTED behavior, not an actual error.
      if (eventSource.readyState === EventSource.CLOSED) {
        console.log('SSE connection closed (expected)');
        return;
      }

      // Actual error — server unreachable, network issue, etc.
      console.error('SSE connection error:', error);
      eventSource.close();
      reject(new Error('SSE connection failed'));
    };
  });
}
```

### Usage Example

```javascript
async function handleDiagnoseClick(sessionId, audioFile, documents) {
  // Step 1: Trigger the diagnosis
  const response = await fetch(`/api/Sessions/${sessionId}/diagnose`, {
    method: 'POST',
    body: formData, // audio + documents
  });

  if (!response.ok) {
    showError('Failed to start diagnosis');
    return;
  }

  // Step 2: Show loading state
  showLoadingSpinner('AI is analyzing your data...');

  try {
    // Step 3: Wait for the SSE notification
    await waitForDiagnosis(sessionId);

    // Step 4: Diagnosis is complete! Fetch the full result
    const resultResponse = await fetch(`/api/Sessions/${sessionId}/result`);
    const result = await resultResponse.json();

    // Step 5: Display the result
    hideLoadingSpinner();
    displayDiagnosisResult(result);

  } catch (error) {
    hideLoadingSpinner();
    showError('Connection lost. Please refresh and check results.');
  }
}
```

---

## React Implementation

```jsx
import { useState, useEffect, useCallback } from 'react';

/**
 * Custom hook that listens for diagnosis completion via SSE
 */
function useDiagnosisStream(sessionId, enabled = false) {
  const [status, setStatus] = useState('idle'); // idle | connecting | waiting | completed | error
  const [error, setError] = useState(null);

  useEffect(() => {
    if (!enabled || !sessionId) return;

    setStatus('connecting');
    const eventSource = new EventSource(
      `${API_BASE_URL}/api/Sessions/${sessionId}/diagnosis-stream`
    );

    eventSource.addEventListener('connected', () => {
      setStatus('waiting');
    });

    eventSource.addEventListener('diagnosis-complete', (event) => {
      setStatus('completed');
      eventSource.close();
    });

    eventSource.onerror = () => {
      if (eventSource.readyState === EventSource.CLOSED) {
        // Server closed connection after sending event — expected
        return;
      }
      setStatus('error');
      setError('Connection to server lost');
      eventSource.close();
    };

    // Cleanup on unmount (e.g., user navigates away)
    return () => {
      eventSource.close();
    };
  }, [sessionId, enabled]);

  return { status, error };
}

/**
 * Example component usage
 */
function DiagnosisPage({ sessionId }) {
  const [diagnosing, setDiagnosing] = useState(false);
  const [result, setResult] = useState(null);
  const { status } = useDiagnosisStream(sessionId, diagnosing);

  // When SSE says "completed", fetch the full result
  useEffect(() => {
    if (status === 'completed') {
      fetch(`${API_BASE_URL}/api/Sessions/${sessionId}/result`)
        .then(res => res.json())
        .then(data => {
          setResult(data);
          setDiagnosing(false);
        });
    }
  }, [status, sessionId]);

  const handleDiagnose = async (formData) => {
    await fetch(`${API_BASE_URL}/api/Sessions/${sessionId}/diagnose`, {
      method: 'POST',
      body: formData,
    });
    setDiagnosing(true); // This enables the SSE hook
  };

  return (
    <div>
      {status === 'waiting' && <Spinner text="AI is analyzing..." />}
      {status === 'completed' && result && <ResultCard data={result} />}
      {status === 'error' && <Alert text="Connection lost. Try refreshing." />}
    </div>
  );
}
```

---

## TypeScript Types

```typescript
// SSE Event Data Types
interface SseConnectedEvent {
  sessionId: number;
}

interface SseDiagnosisCompleteEvent {
  sessionId: number;
  status: 'completed';
}

// Hook return type
interface UseDiagnosisStreamResult {
  status: 'idle' | 'connecting' | 'waiting' | 'completed' | 'error';
  error: string | null;
}
```

---

## Important Notes

### ⚡ Race Condition Handling
> **Q:** What if the callback arrives BEFORE the frontend connects to SSE?
>
> **A:** The backend uses `BoundedChannelOptions` with `DropOldest` mode. If the callback
> fires before any SSE client is listening, the notification is simply discarded.
> **However**, the data is already saved in the database. The frontend should
> implement a fallback:

```javascript
async function handleDiagnoseWithFallback(sessionId) {
  showLoadingSpinner();

  // Start SSE and a polling fallback simultaneously
  const ssePromise = waitForDiagnosis(sessionId);
  const pollingPromise = pollForCompletion(sessionId, 3000); // poll every 3s

  // Whichever resolves first wins
  await Promise.race([ssePromise, pollingPromise]);

  // Fetch and display result
  const result = await fetchResult(sessionId);
  displayResult(result);
}

async function pollForCompletion(sessionId, intervalMs) {
  while (true) {
    const res = await fetch(`/api/Sessions/${sessionId}/status`);
    const data = await res.json();
    if (data.status === 'Completed') return;
    await new Promise(r => setTimeout(r, intervalMs));
  }
}
```

### 🔌 Connection Cleanup
- Always call `eventSource.close()` when done or when the component unmounts
- The React hook handles this via the `useEffect` cleanup function
- Never leave SSE connections open unnecessarily

### 🔒 CORS
The backend already has `AllowAll` CORS policy, so SSE works from any origin. If you restrict CORS later, make sure to allow the `text/event-stream` content type.

### 📱 Browser Support
`EventSource` is supported in all modern browsers:
- ✅ Chrome, Firefox, Safari, Edge
- ❌ Internet Explorer (use a polyfill like `eventsource-polyfill`)

---

## Debugging Tips

### Test with curl
```bash
# Open a terminal and connect to the SSE endpoint
curl -N http://localhost:5000/api/Sessions/1/diagnosis-stream

# You should see:
# event: connected
# data: {"sessionId": 1}
#
# ... (waiting) ...
#
# event: diagnosis-complete
# data: {"sessionId": 1, "status": "completed"}
```

### Test with Browser DevTools
1. Open **Network** tab
2. Filter by **EventStream** or **Fetch/XHR**
3. Trigger the diagnosis
4. You'll see the SSE connection appear as a pending request
5. Click on it → go to **EventStream** tab to see events in real-time

---

## Summary Table

| Step | Who | Action | Endpoint |
|---|---|---|---|
| 1 | Frontend | Trigger diagnosis | `POST /api/Sessions/{id}/diagnose` |
| 2 | Frontend | Open SSE stream | `GET /api/Sessions/{id}/diagnosis-stream` |
| 3 | Backend | Process + wait | — |
| 4 | LangFlow | Send callback | `POST /api/{id}/callback` |
| 5 | Backend | Save + notify SSE | — |
| 6 | Frontend | Receive event | SSE `diagnosis-complete` |
| 7 | Frontend | Fetch full result | `GET /api/Sessions/{id}/result` |
