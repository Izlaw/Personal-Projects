var clock;
function startTime(element) {
    let timeString = new Date().toLocaleTimeString('en-US', { hour: 'numeric', hour12: true, minute: 'numeric', second: 'numeric' });
    let dateString = new Date().toLocaleDateString('en-US', { dateStyle: 'short' });
    element.innerHTML = dateString + " - " + timeString;
    clock = setTimeout(startTime.bind(null, element), 1000);
}
function stopTime() {
    clearTimeout(clock);
}
