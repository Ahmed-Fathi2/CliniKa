const https = require('https');

const url = process.argv[2];
const payload = process.argv[3];

const req = https.request(url, {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(payload)
    }
}, (res) => {
    let data = '';
    res.on('data', (chunk) => data += chunk);
    res.on('end', () => {
        console.log(JSON.stringify({ statusCode: res.statusCode, body: data }));
    });
});

req.on('error', (e) => {
    console.log(JSON.stringify({ statusCode: 500, body: e.message }));
});

req.write(payload);
req.end();