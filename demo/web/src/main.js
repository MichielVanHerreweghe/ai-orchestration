const form = document.querySelector('form');
const list = document.querySelector('#notes');
const heading = document.querySelector('h1');

async function load() {
  const notes = await (await fetch('/api/notes')).json();
  heading.textContent = `Notes (${notes.filter((note) => !note.isDone).length} open)`;
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
    const item = document.createElement('li');
    item.append(box, ' ', text);
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
