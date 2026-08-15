const fs = require('fs');
const path = require('path');

async function main() {
  try {
    const args = process.argv.slice(2);
    // Expected: url sessionId audioPath audioName [docPath1 docName1 docPath2 docName2 ...]
    if (args.length < 4) {
      console.log(JSON.stringify({
        statusCode: 400,
        body: JSON.stringify({ error: 'Missing arguments. Expected: url sessionId audioPath audioName [docPath1 docName1 ...]' })
      }));
      process.exit(1);
    }

    const [url, sessionId, audioPath, audioName] = args.slice(0, 4);
    const docArgs = args.slice(4); // pairs of [docPath, docName]

    const formData = new FormData();
    formData.append('sessionId', sessionId);

    // Add audio file under key 'audio'
    if (fs.existsSync(audioPath)) {
      const audioBuffer = fs.readFileSync(audioPath);
      const ext = path.extname(audioName).toLowerCase();
      let mimeType = 'audio/mpeg';
      if (ext === '.wav') mimeType = 'audio/wav';
      else if (ext === '.m4a') mimeType = 'audio/mp4';
      else if (ext === '.aac') mimeType = 'audio/aac';
      const audioBlob = new Blob([audioBuffer], { type: mimeType });
      formData.append('file', audioBlob, audioName);
    }

    // Add documents under keys image1, image2, etc.
    let docIndex = 1;
    for (let i = 0; i < docArgs.length; i += 2) {
      const docPath = docArgs[i];
      const docName = docArgs[i + 1];
      if (docPath && docName && fs.existsSync(docPath)) {
        const docBuffer = fs.readFileSync(docPath);
        const ext = path.extname(docName).toLowerCase();
        let mimeType = 'application/octet-stream';
        if (ext === '.png') mimeType = 'image/png';
        else if (ext === '.jpg' || ext === '.jpeg' || ext === '.jfif') mimeType = 'image/jpeg';
        else if (ext === '.pdf') mimeType = 'application/pdf';
        const docBlob = new Blob([docBuffer], { type: mimeType });
        formData.append(`image${docIndex}`, docBlob, docName);
        docIndex++;
      }
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
