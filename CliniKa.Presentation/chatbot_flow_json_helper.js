const fs = require('fs');

const urlStr = process.argv[2];
const filePath = process.argv[3]; 

let payload = fs.readFileSync(filePath, 'utf8');
if (payload.charCodeAt(0) === 0xFEFF) {
    payload = payload.slice(1);
}

const parsedUrl = new URL(urlStr);
const httpLib = parsedUrl.protocol === 'https:' ? require('https') : require('http');

const req = httpLib.request(parsedUrl, {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(payload),
        'ngrok-skip-browser-warning': 'true',
        'User-Agent': 'CliniKa-Backend'
    }
}, (res) => {
    let data = '';
    res.on('data', (chunk) => data += chunk);
    res.on('end', () => {
        try { fs.unlinkSync(filePath); } catch (e) { }

        console.log(JSON.stringify({ statusCode: res.statusCode, body: data }));
    });
});

req.on('error', (e) => {
    try { fs.unlinkSync(filePath); } catch (e) { }
    console.log(JSON.stringify({ statusCode: 500, body: e.message }));
});

req.write(payload);
req.end();
