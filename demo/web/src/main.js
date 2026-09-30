import './style.css';

const form = document.querySelector('form');
const list = document.querySelector('#notes');
const count = document.querySelector('#count');

async function load() {
  const notes = await (await fetch('/api/notes')).json();
  count.textContent = `(${notes.filter((note) => !note.isDone).length} open)`;
  list.replaceChildren(...notes.map((note) => {
    const box = Object.assign(document.createElement('input'), { type: 'checkbox', checked: note.isDone });
    box.addEventListener('change', async () => {
      await fetch(`/api/notes/${note.id}`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ isDone: box.checked }),
      });
      await load();
    });
    const text = document.createElement(note.isDone ? 's' : 'span');
    text.textContent = note.text;
    const label = document.createElement('label');
    label.append(box, text);
    const item = document.createElement('li');
    item.className = note.isDone ? 'done' : '';
    item.append(label);
    return item;
  }));
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
