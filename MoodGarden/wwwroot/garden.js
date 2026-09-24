window.garden = {
    getRect: (el) => {
        const r = el.getBoundingClientRect();
        return { left: r.left, top: r.top, width: r.width, height: r.height };
    },
    load: (key) => {
        try { return localStorage.getItem(key); } catch { return null; }
    },
    save: (key, value) => {
        try { localStorage.setItem(key, value); } catch { }
    },
    hour: () => new Date().getHours()
};
