const valueElement = document.getElementById('value');
const errorElement = document.getElementById('error');
const startButton = document.getElementById('start');
const stopButton = document.getElementById('stop');
let source = null;

async function start() {
    errorElement.textContent = '';

    const loginResponse = await fetch('/cookie/same/login', {
        method: 'POST',
        credentials: 'same-origin'
    });

    if (!loginResponse.ok) {
        errorElement.textContent = `Could not initialize same-origin cookie (${loginResponse.status}).`;
        return;
    }

    source = new EventSource('/events-cookie-same');
    startButton.disabled = true;
    stopButton.disabled = false;

    source.onmessage = (event) => {
        valueElement.textContent = event.data;
    };

    source.onerror = () => {
        errorElement.textContent = 'Connection failed or was closed.';
        stop();
    };
}

function stop() {
    if (source) {
        source.close();
        source = null;
    }

    startButton.disabled = false;
    stopButton.disabled = true;
}

startButton.addEventListener('click', start);
stopButton.addEventListener('click', stop);
