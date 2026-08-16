const fs = require('fs');

const url = process.argv[2];
const sessionId = process.argv[3];
const audioPath = process.argv[4];
const audioName = process.argv[5];

async function sendFormData() {
    try {
        const formData = new FormData();

        formData.append('sessionId', sessionId);

        if (fs.existsSync(audioPath)) {
            const audioBuffer = fs.readFileSync(audioPath);
            const audioBlob = new Blob([audioBuffer]);
            formData.append('audio', audioBlob, audioName);
        }

        let i = 6;
        let docIndex = 1;
        while (i < process.argv.length) {
            const docPath = process.argv[i];
            const docName = process.argv[i + 1];

            if (docPath && docName && fs.existsSync(docPath)) {
                const docBuffer = fs.readFileSync(docPath);
                const docBlob = new Blob([docBuffer]);
                formData.append(`document_${docIndex}`, docBlob, docName);
                docIndex++;
            }
            i += 2;
        }

        const response = await fetch(url, {
            method: 'POST',
            body: formData
        });

        const responseText = await response.text();

        console.log(JSON.stringify({ statusCode: response.status, body: responseText }));

    } catch (error) {
        console.log(JSON.stringify({ statusCode: 500, body: error.message }));
    }
}

sendFormData();