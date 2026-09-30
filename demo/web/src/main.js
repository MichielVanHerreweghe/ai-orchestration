const form = document.querySelector('form');
const list = document.querySelector('#notes');

async function load() {
  const notes = await (await fetch('/api/notes')).json();
  list.replaceChildren(...notes.map((note) => Object.assign(document.createElement('li'), { textContent: note.text })));
}

form.addEventListener('submit', async (event) => {
  event.preventDefault();
  await fetch('/api/notes', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ text: form.text.value }),
  });
  form.reset();
  await load();
});

load();
