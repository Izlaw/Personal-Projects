window.audioInterop = {
    _audio: null,
    _dotNetRef: null,

    play: function (url, dotNetRef) {
        if (this._audio) {
            this._audio.pause();
            this._audio.onended = null;
            this._audio.onerror = null;
        }

        this._dotNetRef = dotNetRef;
        const audio = new Audio(url);
        this._audio = audio;

        audio.onended = () => {
            this._dotNetRef?.invokeMethodAsync('OnAudioEnded');
        };

        audio.onerror = () => {
            console.error('[audioInterop] Failed to load:', url);
            this._dotNetRef?.invokeMethodAsync('OnAudioError', url);
        };

        // play() must be called from a user gesture to satisfy browser autoplay policy
        return audio.play().catch(err => console.warn('[audioInterop] play() blocked:', err));
    },

    stop: function () {
        if (this._audio) {
            this._audio.pause();
            this._audio.currentTime = 0;
        }
    }
};
