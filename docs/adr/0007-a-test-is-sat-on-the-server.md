# A test is sat on the server

A play has always been practice. The quiz reaches the browser whole, each question with its
right answer, the browser marks every click, and one request at the end hands in the numbers
chosen. The server scores them again from the stored questions, so a score cannot be posted,
but everything else is in the browser's hands: the answers can be read before the first click,
a closed tab loses the play, and a deadline could only be the browser's own.

A teacher who gives a class a private quiz wants more from it: each student sits it once, sees
no answer before the end, and works against a time the teacher set. None of that can be added
to a play that lives in the browser.

So a quiz can be a test, and a test is taken in a sitting, which the server keeps:

- A sitting is started on the server and is a row of its own. The questions are sent without
  anything that says which answer is right: no correct option, no stored pairs or order, no
  hint, no explanation.
- Each answer is saved as it is given, a row per question. The student can leave and come
  back on another device; the sitting is still there.
- The deadline is a stored time: the quiz's time limit counted from the sitting's start, or
  the test's closing time, whichever comes first.
- When a sitting ends, by the student handing it in, by its deadline or by the teacher closing
  the test, the server writes an ordinary stored play from the saved answers, through the same
  scoring a practice play uses. A point that was not answered is still a row, wrong and with
  no chosen text ([0005](0005-one-answer-row-per-point.md)).
- There is no background job. A sitting whose time has run out is ended by the next request
  that reads it: the student's own, the teacher's results, the student's list of plays. Its
  play is dated at the deadline, not at the request.

## The numbering

Hiding the correct option is enough for a four-option question. It is not enough for the other
two kinds, because the numbers a play is handed in with are the answer: pair 1 goes with
right-hand option 1, and the items of an ordering question are numbered in their right order.

So the server shuffles those lists once, when the sitting starts, and stores the order it
showed. The student's numbers refer to the lists as shown, and the server translates them back.

A matching question stores the pairs (Judita, Marulić), (Planine, Zoranić), (Osman, Gundulić)
and the extra option Lucić. The stored right-hand numbers are 1 Marulić, 2 Zoranić, 3 Gundulić,
4 Lucić. For one sitting the server shuffles them to `[3, 1, 4, 2]` and sends Gundulić,
Marulić, Lucić, Zoranić. The student links Judita to the second option shown, leaves Planine
open and links Osman to the first: `[2, 0, 1]`. Read back through the stored order that is
option 1, nothing, option 3: Judita right, Planine unanswered, Osman right.

## Considered options

- **Send the questions without the correct option and keep the stored numbering.** Enough for
  four options, and it gives the other two kinds away.
- **A random id for every option, stored on the question.** The same ids for every student,
  so one student's finished test would tell the next which id is which.
- **One JSON document of answers per sitting.** Two answers saved at the same moment would
  each write the whole document, and one would be lost. A row per question cannot collide
  with another question's.
- **A state on the stored play instead of a second table.** A stored play has a score and a
  row for every point; a running one has neither, and every query over plays would have to
  leave it out.
- **A background job for deadlines.** Something to deploy, run and watch, for a result that
  does not depend on when it is computed.
- **A separate "sitting of a class" with its own code**, so one test serves several classes.
  More than the feature needs: a copy of the quiz does it.

## Consequences

- A read can write. The request that notices an expired sitting ends it, so a `GET` may store
  a play. Two requests that notice at once cannot both store one: ending a sitting is a single
  save that checks the sitting's row version and deletes its saved answers, so the second
  save fails whole and stores nothing.
- There is no grace at the deadline. An answer that arrives after it is refused.
- A test's questions are locked from its first sitting, so every student sits the same test.
  Its title, time limit and closing time stay editable.
- A quiz cannot change between test and practice once it has results. A copy can.
- The same sitting is offered for a public quiz, as a mock the student chooses. It hides
  nothing: the quiz and its answers can still be read through the practice address. It is a
  way to work without feedback, not a defence.
- A test stops what the server can stop: reading the answers, posting a score, stopping the
  clock, sitting twice from one account. It does not stop a student from looking things up,
  or from sitting again from a second account.
