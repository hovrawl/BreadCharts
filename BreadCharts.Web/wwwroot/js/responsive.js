window.responsive = (function () {
    const listeners = new Map();

    function registerMq(dotnetRef, query) {
        const mql = window.matchMedia(query);
        const handler = (e) => dotnetRef.invokeMethodAsync('OnMqChange', e.matches);

        mql.addEventListener('change', handler);
        listeners.set(dotnetRef, { mql, handler });

        // Fire once to set the initial state
        dotnetRef.invokeMethodAsync('OnMqChange', mql.matches);
    }

    function unregisterMq(dotnetRef) {
        const rec = listeners.get(dotnetRef);
        if (rec) {
            rec.mql.removeEventListener('change', rec.handler);
            listeners.delete(dotnetRef);
        }
    }

    return { registerMq, unregisterMq };
})();
