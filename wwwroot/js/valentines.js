window.valentines = {
    _stickyEmojis: [],
    _heartTimeout: null,
    _heartsActive: false,

    stopHearts: function () {
        window.valentines._heartsActive = false;
        if (window.valentines._heartTimeout) {
            clearTimeout(window.valentines._heartTimeout);
            window.valentines._heartTimeout = null;
        }
        window.valentines._stickyEmojis.forEach(e => e.remove());
        window.valentines._stickyEmojis = [];
    },

    spawnHearts: function (count) {
        window.valentines._heartsActive = true;
        let heartsPerSpawn = 10;
        let interval = 50;

        function spawnBatch() {
            if (!window.valentines._heartsActive) return;
            for (let i = 0; i < heartsPerSpawn; i++) {
                const el = document.createElement('div');
                el.textContent = '❤️';
                el.style.cssText = `
                    position: fixed;
                    font-size: 2.5rem;
                    pointer-events: none;
                    z-index: 9999;
                    left: ${Math.random() * 100}vw;
                    bottom: 0;
                    animation: vp-fly-up ${3.5 + Math.random() * 2}s linear forwards;
                `;
                document.body.appendChild(el);
                setTimeout(() => el.remove(), 6000);
            }

            if (heartsPerSpawn > 1) heartsPerSpawn -= 0.3;
            if (interval < 1000) interval += 5;
            window.valentines._heartTimeout = setTimeout(spawnBatch, interval);
        }

        // Clear any lingering sad emojis
        window.valentines._stickyEmojis.forEach(e => e.remove());
        window.valentines._stickyEmojis = [];

        spawnBatch();
    },

    spawnEmojis: function (count) {
        const emojis = ['😢', '😥', '😭', '😩', '😿', '💔', '😖', '😣', '🥺', '😫'];
        const emojiSize = 48;
        count = count || 5;

        // Get the Yes button's safe zone (generous padding)
        const yesBtn = document.querySelector('.vp-btn-yes');
        let avoidRect = null;
        if (yesBtn) {
            const r = yesBtn.getBoundingClientRect();
            const pad = 70;
            avoidRect = {
                left:   r.left   - pad,
                top:    r.top    - pad,
                right:  r.right  + pad,
                bottom: r.bottom + pad
            };
        }

        function overlapsAvoid(x, y) {
            if (!avoidRect) return false;
            return (
                x + emojiSize > avoidRect.left &&
                x             < avoidRect.right &&
                y + emojiSize > avoidRect.top &&
                y             < avoidRect.bottom
            );
        }

        function randomPos() {
            return {
                x: Math.random() * (window.innerWidth  - emojiSize),
                y: Math.random() * (window.innerHeight - emojiSize)
            };
        }

        for (let i = 0; i < count; i++) {
            let pos = randomPos();
            let attempts = 0;

            // Retry up to 20 times to find a safe spot
            while (overlapsAvoid(pos.x, pos.y) && attempts < 20) {
                pos = randomPos();
                attempts++;
            }

            const el = document.createElement('div');
            el.textContent = emojis[Math.floor(Math.random() * emojis.length)];
            el.style.cssText = `
                position: fixed;
                font-size: ${emojiSize}px;
                pointer-events: none;
                z-index: 9999;
                left: ${pos.x}px;
                top: ${pos.y}px;
                user-select: none;
            `;
            document.body.appendChild(el);
            window.valentines._stickyEmojis.push(el);
        }
    }
};

// Inject keyframes if not already present
if (!document.getElementById('vp-keyframes')) {
    const style = document.createElement('style');
    style.id = 'vp-keyframes';
    style.textContent = `
        @keyframes vp-fly-up {
            0%   { transform: translateY(0) scale(1); opacity: 1; }
            100% { transform: translateY(-110vh) scale(1.5); opacity: 0; }
        }
    `;
    document.head.appendChild(style);
}
