const fs = require('fs');

async function main() {
  try {
    const args = process.argv.slice(2);
    if (args.length < 6) {
      console.log(JSON.stringify({
        statusCode: 400,
        body: JSON.stringify({ error: "Missing arguments. Expected: url sessionId audioPath audioName labPath labName" })
      }));
      process.exit(1);
    }

    const [url, sessionId, audioPath, audioName, labPath, labName] = args;

    const formData = new FormData();
    formData.append('sessionId', sessionId);

    if (fs.existsSync(audioPath)) {
      const audioBuffer = fs.readFileSync(audioPath);
      // Determine content type roughly by extension
      const isPng = audioName.toLowerCase().endsWith('.png');
      const audioBlob = new Blob([audioBuffer], { type: isPng ? 'image/png' : 'audio/mpeg' });
      formData.append('audio', audioBlob, audioName);
    }

    if (fs.existsSync(labPath)) {
      const labBuffer = fs.readFileSync(labPath);
      const isPng = labName.toLowerCase().endsWith('.png');
      const labBlob = new Blob([labBuffer], { type: isPng ? 'image/png' : 'image/jpeg' });
      formData.append('lab1', labBlob, labName);
    }

    const response = await fetch(url, {
      method: 'POST',
      body: formData
    });

    const responseText = await response.text();
    
    console.log(JSON.stringify({
      statusCode: response.status,
      body: responseText
    }));
  } catch (err) {
    console.log(JSON.stringify({
      statusCode: 500,
      body: JSON.stringify({ error: err.message })
    }));
  }
}

main();
