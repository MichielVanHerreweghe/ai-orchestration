import { Component, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

interface Note {
  id: number;
  text: string;
  isDone: boolean;
}

@Component({
  selector: 'app-root',
  template: `
    <main>
      <h1>Notes <span id="count">({{ open() }} open)</span></h1>
      <form (submit)="add($event, text)">
        <input #text name="text" required placeholder="Write a note" aria-label="Note text" />
        <button>Add</button>
      </form>
      <ul id="notes">
        @for (note of notes(); track note.id) {
          <li [class.done]="note.isDone">
            <label>
              <input type="checkbox" [checked]="note.isDone" (change)="toggle(note, $event)" />
              @if (note.isDone) {
                <s>{{ note.text }}</s>
              } @else {
                <span>{{ note.text }}</span>
              }
            </label>
          </li>
        }
      </ul>
    </main>
  `,
})
export class App {
  private readonly http = inject(HttpClient);
  protected readonly notes = signal<Note[]>([]);
  protected readonly open = computed(() => this.notes().filter((n) => !n.isDone).length);

  constructor() {
    this.load();
  }

  private load() {
    this.http.get<Note[]>('/api/notes').subscribe((notes) => this.notes.set(notes));
  }

  protected add(event: Event, input: HTMLInputElement) {
    event.preventDefault();
    this.http.post('/api/notes', { text: input.value }).subscribe(() => {
      input.value = '';
      this.load();
    });
  }

  protected toggle(note: Note, event: Event) {
    const isDone = (event.target as HTMLInputElement).checked;
    this.http.patch(`/api/notes/${note.id}`, { isDone }).subscribe(() => this.load());
  }
}
