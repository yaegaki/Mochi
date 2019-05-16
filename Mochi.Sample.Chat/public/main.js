(() => {
    const chatElem = document.getElementById('chat');
    const formElem = document.getElementById('form');
    const nameElem = document.getElementById('name');
    const textElem = document.getElementById('text');

    async function submit() {
        try
        {
            if (text.value.length === 0) return;

            nameElem.setAttribute('readonly', 'readonly');
            textElem.setAttribute('readonly', 'readonly');

            const form = new FormData();
            form.append('name', nameElem.value.replace(/[,:]/g, '_'));
            form.append('text', textElem.value);
            fetch('/post', { method: 'POST', body: form });
            text.value = '';
        }
        finally
        {
            nameElem.removeAttribute('readonly');
            textElem.removeAttribute('readonly');
        }
    }

    formElem.addEventListener('submit', e => {
        e.preventDefault();

        submit();
    });

    const es = new EventSource('/sse');
    const texts = [];
    es.addEventListener('message', e => {
        const data = e.data.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/""/g, '&quot;').replace(/'/g, '&#39;');

        const index = data.indexOf(',');
        if (index < 0) {
            return;
        }
        const name = data.substr(0, index);
        const text = data.substr(index + 1);
        texts.unshift(`<div><span class="name">${name}</span>:<span class="text">${text}</span></div>`);
        requestAnimationFrame(() => {
            if (chatElem.innerHTML.length > 5000) chatElem.innerHTML = '<div class="flush">Flush...</div>';
            chatElem.innerHTML = `<div>${texts.join('')}</div>${chatElem.innerHTML}`;
            texts.length = 0;
        })
    });
    es.addEventListener('error', e => {
        chatElem.innerHTML = `<div class="error">Error occurred!</div>${chatElem.innerHTML}`;
    });
})();