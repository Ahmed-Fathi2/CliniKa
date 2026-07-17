const fs = require('fs');

async function run() {
  const url = 'https://cloud.activepieces.com/api/v1/webhooks/Jg8uqP6OMXCpUsatpxUzt/sync';
  const audioPath = 'f:\\CliniKa-BackEnd\\CliniKa\\CliniKa.Presentation\\wwwroot\\uploads\\doc-7dee08d1-3659-4ac9-ab86-02a8052ee052.png';
  const labPath = 'f:\\CliniKa-BackEnd\\CliniKa\\CliniKa.Presentation\\wwwroot\\uploads\\doc-7dee08d1-3659-4ac9-ab86-02a8052ee052.png';

  const formData = new FormData();
  formData.append('sessionId', '1');

  const audioBuffer = fs.readFileSync(audioPath);
  const audioBlob = new Blob([audioBuffer], { type: 'audio/mpeg' });
  formData.append('audio', audioBlob, 'audio.mp3');

  const labBuffer = fs.readFileSync(labPath);
  const labBlob = new Blob([labBuffer], { type: 'image/jpeg' });
  formData.append('lab1', labBlob, 'lab1.jpg');

  console.log('Sending request to Activepieces...');
  try {
    const response = await fetch(url, {
      method: 'POST',
      body: formData
    });
    console.log('Status Code:', response.status);
    const text = await response.text();
    console.log('Response:', text);
  } catch (error) {
    console.error('Error:', error);
  }
}

run();
