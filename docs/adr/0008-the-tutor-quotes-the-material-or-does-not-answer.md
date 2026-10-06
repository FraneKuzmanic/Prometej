# The tutor quotes the material or does not answer

The learning material is twelve texts, about 25,000 words. A student who wants to know where
something is said has a search that matches letters. A tutor that can be asked in plain
words is the obvious next thing, and a language model can be one.

A language model asked directly answers from what it remembers, fluently and without a
source. For school material that is the wrong way round. A confident sentence that is not
quite right about a set book is worse than no sentence, because the student cannot tell the
two apart, and the material itself, which was written and checked for this purpose, is not
what answered.

So the tutor, "Prometej" on screen, answers only from the material, and has to show where:

- The model (GPT-4.1 on Azure OpenAI) does not get the texts in its prompt. It gets three
  tools: search the material, list a period's sections, read one section. The tools read
  through this application's own public endpoints, the ones the browser uses, so the data has
  one owner and the tutor reads what the student reads.
- Its first move has to be a tool call. Asked for an answer in a fixed shape, the model
  otherwise gives one at once, from memory.
- The answer has a fixed shape: a kind, a text, and citations. A citation is a period, a
  section and a quote. The kind is `answer`, `not_covered` (the material does not say) or
  `declined` (an essay, homework, something else entirely).
- Before anything is shown, every quote is looked for in the section it names, word for
  word. Unicode composition and runs of whitespace are evened out; nothing else is. A quote
  is 15 to 400 characters, and an `answer` needs at least one.
- An answer that fails is sent back to the model once, with what failed. If it fails again
  the student gets one fixed line, "Nisam uspio potkrijepiti odgovor gradivom", and no part
  of the answer.
- A section is an `h2` or an `h3` of the text and its id is the one the period page gives
  that heading, so a citation is an address: a click opens the period at that heading.
- The model's code is a small Python service (`ai/`). The browser talks to the .NET API as
  before, which checks the shape of a question, limits how often one caller may ask, and
  forwards it.

## Measured, with the bars written first

Whether this works is a number, not an impression. Four bars were written down and committed
before the first run (`ai/evals/bars.json`), and the runs are scored by a script, not by a
model:

- the demo quizzes' four-option questions answered right, with tools, at least 90%;
- the same questions without tools as the baseline, and the claim that the material is what
  answers them fails if the tools add fewer than ten points;
- questions asked without naming a period, the right period and section cited, at least 85%;
- questions the material does not cover, or that ask for homework, refused, at least 90%.

The measured sets are not the ones the prompt was tried on (`dev.jsonl` is for that). Every
recorded run is a file in `ai/evals/results`, and a run is not repeated for a better number.
The README has the table.

## Considered options

- **Embeddings and a vector index.** The usual answer to "find the relevant text". Twelve
  texts with headings can be read by section, and a search by letters is already there. An
  index would be a second copy of the material to keep in step, for a problem not yet
  measured to exist. It stays the next step if a bar is missed for lack of it.
- **Calling the model from the .NET API.** One program less. The eval, the prompt and the
  checks would then be C#, further from the tools this kind of work is usually done with,
  and the API would hold a key it does not otherwise need.
- **The texts in the prompt, no tools.** All twelve fit in the model's context. Every
  question would then pay for 25,000 words, and nothing would say which section an answer
  came from.
- **Streaming the answer.** It reads better. A quote cannot be checked before it has
  arrived, so a streamed answer would be shown first and withdrawn after.
- **Answers "beyond the material", labelled as such.** The label can be made reliable. What
  is under it cannot be checked.
- **A second model as the judge of the eval.** It scales to answers a script cannot score,
  and its verdicts would need measuring themselves. The four bars are things a script can
  count.
- **A cache of the parsed texts.** The texts are small, and an admin's edit should hold for
  the next question. Within one question a period is read once, so a quote is checked
  against the text the model was given.

## Consequences

- **The quote is checked. The sentence around it is not.** The tutor can still explain a
  true quote wrongly. The first bar is what measures that, on 24 questions; the note under
  the panel tells the student to read the cited place.
- The tutor is slower than a chat: a few requests to the model per question, about five
  seconds, and nothing is shown until the check has passed.
- Anyone can ask, signed in or not, ten times a minute for an account or an address. Each
  question costs money, so a deployed instance needs a cap on spending that this one does
  not have.
- The tutor does not know about tests. A student sitting one can ask it; a refusal tied to
  the session would be undone by a private window.
- The conversation is the browser's. The server keeps none of it and logs only numbers per
  answer (the kind, the tools called, tokens, time), never a question or an answer. A turn
  of history is whatever the browser sends, so a forged one changes only the forger's answer.
- The rule that turns a heading into a section id is written twice, in the client and in the
  Python service. A test ties the Python side to a seeded text.
- The service listens on localhost and trusts its caller.
- Without the service, or without its address in the API's configuration, the tutor's button
  is not shown and everything else works as before.
