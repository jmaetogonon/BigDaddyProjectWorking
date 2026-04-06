window.bigdaddy = {

    // Set a browser cookie (shared across tabs)
    setCookie: (name, value, minutes) => {
        const d = new Date();
        d.setTime(d.getTime() + minutes * 60 * 1000);
        document.cookie =
            `${name}=${encodeURIComponent(value)};expires=${d.toUTCString()};path=/;SameSite=Strict`;
    },

    // Remove a cookie
    removeCookie: (name) => {
        document.cookie = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 UTC;path=/;`;
    },

    // Read a cookie value
    getCookie: (name) => {
        const v = document.cookie.match('(^|;)\\s*' + name + '\\s*=\\s*([^;]+)');
        return v ? decodeURIComponent(v.pop()) : null;
    },

    // Toggle browser fullscreen
    toggleFullscreen: () => {
        if (!document.fullscreenElement)
            document.documentElement.requestFullscreen?.();
        else
            document.exitFullscreen?.();
    },

    // Export a <table> element to CSV download
    exportTableToCSV: (tableId, filename) => {
        const table = document.getElementById(tableId);
        if (!table) return;
        const csv = [...table.querySelectorAll('tr')]
            .map(row =>
                [...row.querySelectorAll('th,td')]
                    .map(c => `"${c.textContent.replace(/"/g, '""')}"`)
                    .join(',')
            ).join('\n');
        const a = document.createElement('a');
        a.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }));
        a.download = filename || 'export.csv';
        a.click();
    }
};

// On every page load: if no access_token cookie exists,
// clear stale sessionStorage so the UI shows login page.
// (sessionStorage survives F5 refresh within the same tab)
(function () {
    if (!window.bigdaddy.getCookie('access_token'))
        sessionStorage.removeItem('userInfo');
})();