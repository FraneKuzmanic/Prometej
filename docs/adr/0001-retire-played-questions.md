# A question removed from a played quiz is retired, not deleted

A stored play (a quiz game) keeps one answer row per question, and its score only means
something next to the number of questions it was played with. Deleting a question deletes its
answers through the foreign key, so removing one question from a quiz would silently turn every
earlier `3 / 3` into `2 / 2`. When a teacher removes a question that has at least one stored
answer, the question is therefore marked `IsRetired` instead: it no longer appears in the editor
or in new plays, and the plays that answered it keep it. A question nobody has answered has no
history to protect and is deleted outright.

## Considered options

- **Delete the question and its answers.** The simplest code, and the reason for this record:
  it rewrites results students already have.
- **Refuse to edit a quiz once it has been played.** Protects the results, but a teacher who
  spots a bad question after the first class could never fix the quiz.
- **Version the whole quiz** and attach each play to the version it was played against. The
  complete answer, and far more machinery than a quiz of four-option questions needs today.
- **Keep the answers and null their question reference.** The play keeps its count, but the
  answer no longer says what it was an answer to.

## Consequences

- Every query that serves a quiz for editing or playing filters retired questions out; the
  analytics query does not.
- A retired question cannot be brought back. An update that names its id is refused the same
  way as an id from another quiz.
- Retired questions are only removed when their quiz is deleted.
