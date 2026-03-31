window.plannerSessionStorage = {
    getInt(key) {
        const value = window.sessionStorage.getItem(key);
        if (value === null || value === undefined || value === "") {
            return null;
        }

        const parsed = Number.parseInt(value, 10);
        return Number.isNaN(parsed) ? null : parsed;
    },
    setInt(key, value) {
        if (value === null || value === undefined) {
            window.sessionStorage.removeItem(key);
            return;
        }

        window.sessionStorage.setItem(key, String(value));
    },
    clear(key) {
        window.sessionStorage.removeItem(key);
    }
};
