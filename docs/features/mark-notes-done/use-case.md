# Use case: Mark notes as done

## Summary
A user ticks off notes they have handled. Done notes stay in the list, struck through and below the open ones. The heading shows how many notes are still open. The done state is stored in the database, so it survives a reload.

## Actors
| Actor | Role |
|-------|------|
| User | Adds notes and ticks or unticks them |
| Web (`demo/web`) | Renders the list and heading, sends changes |
| API (`demo/api`) | Stores and returns the notes |

## Preconditions
- The demo app is running (API, SQL Server, web).
- For the ticking flows: at least one note exists. For the ordering flow: at least three notes, all open.

## Main flow
1. The user opens the page. The heading reads "Notes (N open)", N being the number of open notes.
2. The user adds three notes. The heading reads "Notes (3 open)".
3. The user ticks the checkbox of one note.
4. The note is saved as done, moves below the two open notes and is shown struck through. The heading reads "Notes (2 open)".
5. The user reloads the page. The ticked note is still done, still below the open ones.

## Alternate flows
- **Untick**: the user unticks a done note. It becomes open again, loses the strike-through, moves back among the open notes (newest first) and the heading reads "Notes (3 open)".
- **No notes**: the heading reads "Notes (0 open)".
