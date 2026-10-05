# The correct answer is stored as an option number

A question has four options and one of them is correct. The correct one used to be stored as a
copy of its text, which made "which option is right" depend on two strings staying equal:
editing the option's text left the old copy behind, and the question could no longer be answered
correctly by anyone. A question now stores `CorrectOption`, a number from 1 to 4, and a play is
submitted as the number of the option chosen for each question.

The answers stored with a play are the exception, on purpose. Each one keeps the text of the
option that was chosen and the text of the option that was correct at that moment. A question
can be edited after it was played; the number says what is right now, the texts say what the
student saw then, so an old play never has to be re-read against a question that has since
changed.

Since players can read their own plays again, an answer also keeps the question's title and its
explanation as they were, for the same reason. Answers stored before that were given the title
and explanation their question had on the day of the migration, the only wording still known.

## Considered options

- **Keep the text and update the copy whenever an option is edited.** Fixes the editor, not the
  model: any other writer of the table can break it again, and two options with the same text
  are ambiguous.
- **A separate table of options with a foreign key to the correct one.** The right shape if
  questions ever have a variable number of options. With exactly four it adds a join and a
  circular reference for no gain.

## Consequences

- The migration maps each stored text to the first option that matches it. A question whose
  stored text matched no option is given option 1, because the column cannot be empty; such
  questions were already unanswerable and need their correct option set by hand.
- The server compares numbers, so the four options of a question must differ. That is validated
  when a quiz is saved.
