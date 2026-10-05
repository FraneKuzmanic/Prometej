# A point is an answer row

A quiz used to be a list of four-option questions, a play one answer row per question, and the
score the number of right rows. Every reader divided the score by the number of rows: the
results list, the progress per period, the chart and the summary per student.

Questions now come in three types. A choice question has four options and one correct. A
matching question has three to five pairs, and gives a point for each right pair. An ordering
question has three to six items, and gives a point for each item at its place. A question also
may be asked about a source text, a passage shown beside it.

A play stores **one answer row for each point it could win**: one for a choice question, one
for each pair, one for each place. The score is still the number of right rows and the maximum
still the number of rows. No stored result was migrated, and nothing that divides one by the
other had to change.

The type is a column on the question. The four option columns stay for choice questions and
are null for the others, whose pairs or items are one JSON document on the question. A source
text is a row of its own that questions point to. An answer points to the source text it was
played beside, and a source text that was played is never changed: an edit stores a new row
and retires the old one, as a played question is retired
([0001](0001-retire-played-questions.md)).

## Considered options

- **One row per question, with the answer as JSON and a stored maximum on the play.** The
  maximum would need a backfill for every stored play, and "how many" would mean two things:
  rows for old plays, a column for new ones. Counting how often one pair was missed would mean
  reading into the JSON.
- **A table of options with a foreign key to the right one.** The shape
  [0002](0002-correct-answer-as-option-number.md) set aside for four options. Pairs and items
  are read and replaced only together with their question, so rows of their own would add
  joins and nothing to query by.
- **Moving the four options into the JSON document too.** One shape for every type, at the
  price of a data migration of every stored question and of the plays' scoring code, to tidy
  columns that work.
- **Copying the source text onto every answer**, as the question's title is copied. A passage
  can be eight thousand characters and is shared by up to ten questions; a play would store it
  ten times.

## Consequences

- A row of a matching question names its left item and a row of an ordering question its
  place. A review reads only answer rows, never the question of today, and tells the kind of
  question from those two columns.
- Right and wrong are decided by comparing numbers when a play is submitted. The two texts
  kept on a row say the same afterwards, because the texts a number can stand for must all
  differ. That is validated when a quiz is saved.
- An ordering question is scored by place, so one item put too early moves the others off
  their places and costs their points too. It is simple to explain and harsh on a near miss.
- The report per question counts points. Its lines for a matching or an ordering question are
  grouped by the item or place as played, so an item that was reworded shows as two lines.
- A question's type cannot be changed: its stored answers have the rows of the type it was
  played as.
- The JSON document is replaced whole on every edit and never changed in place.
