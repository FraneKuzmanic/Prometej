# Prometej

A web platform for learning Croatian literature. The learning material is organised by
literary period; quizzes let students check what they read and let teachers see how a class did.

It started in 2024 as my final paper at university, written with the idea that my high-school
teachers would author the material. The [paper](https://drive.google.com/file/d/1dKCoy6_ElFA_kVEj95mHxvKKT-1d5zTl/view?usp=drive_link)
(in Croatian, with screenshots) describes that first version. Since then the project has been
reworked: real authentication, validated quizzes, stored results, search, and the twelve
periods of the national exam catalogue.

## What it does

**Anyone**, without an account

- reads the material of a period, with a contents list that follows the reading, and searches
  it; the search ignores case and Croatian diacritics, so "senoa" finds "Šenoa"
- plays public quizzes, or a private practice quiz with the five-digit entry code a teacher
  gave the class
- answers three kinds of question: four options, matching pairs, and putting items in order,
  with a point for every right pair or place
- reads the poem or excerpt a question is asked about beside the question, the form most of
  the national exam's reading and literature tasks take
- can open a hint before answering and, where the quiz has one, reads an explanation after it
- reads the discussion of a period: topics, and the replies under each
- asks Prometej, a tutor that answers from the material only: each answer names the sections
  it rests on and quotes them, a click opens the period at that heading, and when the
  material does not cover a question, or the question is "write my essay", it says so

**Anyone signed in** (registering creates a student account)

- has every play stored, and sees it again under "Moji rezultati", answer by answer
- sees their progress per period
- sits a test a teacher gave the class: once, with no answer shown before the end, the
  answers saved as they are given, and against the server's clock if the teacher set a time
- solves any public quiz the same way, as a mock: every question open at any time, nothing
  marked until it is handed in
- opens a topic in a period's discussion or replies to one; a teacher's and an admin's post
  is marked as such
- deletes their own reply, and their own topic while nobody has replied to it
- changes their name and password

**A teacher**

- writes quizzes, public or private, optionally tied to a period, mixing four-option, matching
  and ordering questions, and can put a source text above a run of questions
- sees how a quiz was played: every play and its answers, each question with how often it was
  answered right and the wrong answer chosen most often (pair by pair and place by place for
  the other two kinds), and each student's number of plays, first and best result
- makes a private quiz a test, with an optional time limit and closing time; sees who has
  started and how each sitting stands, closes the test (which opens the answers to the
  students), and lets one student sit it again
- copies a quiz, to give the same test to a second class

**An admin**

- writes the material of each period in a rich-text editor
- can write quizzes like a teacher, and edit, delete or read the results of any teacher's quiz
- makes a student a teacher or an admin, and back, from a list of the accounts
- removes any topic or reply from a discussion

The interface is in Croatian.

## How it is built

```
backend/     .NET 8, ASP.NET Core, EF Core, PostgreSQL
  Prometej_api/            host, controllers, authentication, seed content
  Prometej_core/           entities, request and view models, services
  Prometej_persistance/    DbContext, migrations, repository
  Prometej_tests/          integration tests
frontend/    React 18, TypeScript, Vite, Redux Toolkit, MUI
ai/          Python 3.13, FastAPI: the tutor
  tutor/                   the prompt, three tools, the loop, the quote check
  evals/                   the sets, the bars, the runner and every recorded result
  tests/                   pytest, against a scripted model
```

A few things worth knowing before reading the code:

- **The session is a JWT in an `HttpOnly`, `Secure`, `SameSite=Lax` cookie.** The page's script
  never sees it. The client calls a relative `/api`, which the dev server proxies to the API,
  so the browser sees one origin. The token only says who is calling: their role, and whether
  the session still holds, are read from the account on every request
  ([0004](docs/adr/0004-a-session-is-checked-against-the-stored-account.md)).
- **The server trusts nothing it can compute.** Who is calling comes from the token and their
  role from the database. A play is submitted as numbers (the option chosen, the pairs made,
  the order given); the server builds the result from the stored questions and ignores a score
  in the request.
- **Old results never change.** An answer keeps the texts it was played with, and a question
  removed from a played quiz is retired instead of deleted, so editing a quiz does not rescore
  the plays before the edit. A source text that was played is kept the same way: an edit
  stores a new version, and the review of an old play shows the one that was read.
- **A point is an answer row.** A play stores a row for each point it could win: one for a
  four-option question, one for each pair, one for each place. The score is the number of
  right rows, as it was when every question was worth one, so no stored result was migrated
  ([0005](docs/adr/0005-one-answer-row-per-point.md)).
- **Errors are typed.** Services throw `NotFoundException`, `ForbiddenException` and the like;
  one handler maps them to status codes. Controllers have no `try/catch`.
- **A retried submit stores one play.** The client sends a key per play, and a unique index
  is the guarantee.
- **A test is sat on the server.** A practice play lives in the browser until it is handed in.
  A test's questions reach a student only inside a sitting, without their answers; the lists
  of a matching and an ordering question are shuffled by the server, which reads the student's
  numbers back through the order it showed. A sitting whose time ran out is ended by the next
  request that reads it, and its result is an ordinary stored play
  ([0007](docs/adr/0007-a-test-is-sat-on-the-server.md)).
- **A post outlives its author's account.** A topic holds what other people answered, so
  deleting an account leaves its posts in place without a name, and an author can delete a
  topic only while it has no replies
  ([0006](docs/adr/0006-a-post-outlives-its-authors-account.md)). Who may delete a post is
  the server's answer, sent with the post. One account can post five times a minute.
- **Entry codes are rate limited.** A request that carries a code is limited to sixty a minute
  for an account, or for an address when nobody is signed in.
- **The tutor quotes the material or does not answer.** A language model (GPT-4.1 on Azure
  OpenAI) reads the period texts through three tools: search, list a period's sections, read
  a section. Its answer has a fixed shape, and every quote in it is looked for, word for
  word, in the section it names before anything is shown. A failed answer goes back to the
  model once; after a second failure the student gets a fixed line and no answer. The model's
  code is a small Python service the browser never talks to: the .NET API validates a
  question, rate limits it and forwards it
  ([0008](docs/adr/0008-the-tutor-quotes-the-material-or-does-not-answer.md)).

Decisions with a longer story are in [`docs/adr`](docs/adr):

- [0001](docs/adr/0001-retire-played-questions.md): a question removed from a played quiz is retired, not deleted
- [0002](docs/adr/0002-correct-answer-as-option-number.md): the correct answer is stored as an option number
- [0003](docs/adr/0003-period-ids-are-not-their-order.md): a period's id is not its place in the curriculum order
- [0004](docs/adr/0004-a-session-is-checked-against-the-stored-account.md): a session is checked against the stored account
- [0005](docs/adr/0005-one-answer-row-per-point.md): a point is an answer row, whatever the type of question
- [0006](docs/adr/0006-a-post-outlives-its-authors-account.md): a post in a discussion outlives its author's account
- [0007](docs/adr/0007-a-test-is-sat-on-the-server.md): a test is sat on the server, in a sitting
- [0008](docs/adr/0008-the-tutor-quotes-the-material-or-does-not-answer.md): the tutor quotes the material or does not answer

## How the tutor is measured

The bars were written down before the first run, and the commit that holds them
(`ai/evals/bars.json`, with the sets) is older than the one that holds the results. A script
scores every run; no model judges another.

| Set | Items | Bar | Result | | Needed the retry | Never shown |
| --- | --- | --- | --- | --- | --- | --- |
| A. Quiz questions, with tools | 24 | at least 90.0% | 24 (100.0%) | pass | 2 | 0 |
| A. The same questions, no tools (baseline) | 24 |  | 20 (83.3%) |  |  |  |
| B. Where is this covered | 36 | at least 85.0% | 36 (100.0%) | pass | 0 | 0 |
| C. Not covered, or homework | 15 | at least 90.0% | 15 (100.0%) | pass | 0 | 0 |
| A. What the tools add | | at least 10 points | +16.7 points | pass | | |

One run of each set, on 6 October 2026, with GPT-4.1 (`2025-04-14`) at temperature 0. The
four runs used about 487,000 tokens; an answer took about five seconds (median).

- **Set A** is the 24 four-option questions of the three sample quizzes that are not asked
  about a poem. The tutor gets the question with its options and has to name one, through the
  same loop and the same quote check as in the app. The baseline is the same model with no
  tools and no material. Its four misses are facts particular to these texts, such as the
  year of a collection.
- **Set B** is 36 questions asked without naming a period ("Čime otac gađa Gregora Samsu?").
  An item is right when a checked citation is in the expected period and section. About a
  third use a name in another case or a paraphrase, because the search matches letters.
- **Set C** is ten questions about works the material does not cover and five that ask for
  an essay, homework or something else. An item is right when the tutor does not answer.
- **Needed the retry** counts answers whose first try failed the quote check and whose
  second passed. **Never shown** counts answers that failed twice.

What the table does not show:

- **The quote is checked, the wording around it is not.** Set A is the only measure of
  whether the explanation is right, and it is 24 questions.
- The sets are small and I wrote them, as I wrote the prompt. Set A's questions were written
  from these same texts. Three results of 100% say that these sets do not find where the
  tutor fails, not that it does not.
- One model, one run. A second run could differ by an item or two.
- Nothing here measures a conversation with follow-up questions, or a student trying to talk
  the tutor out of its rules.

`ai/evals/results` holds every recorded run, item by item.

## Running it locally

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0),
[Node.js](https://nodejs.org/) and PostgreSQL. Docker is needed only for the tests.

### 1. Configuration

No secret is kept in a tracked file. From `backend/`, set the connection string, a signing key
for the session token (base64 of at least 32 random bytes, for example the output of
`openssl rand -base64 48`) and the first admin account:

```
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Username=postgres;Password=<password>;Database=prometej" --project Prometej_api
dotnet user-secrets set "Jwt:Key" "<base64 key>" --project Prometej_api
dotnet user-secrets set "Seed:Users:0:Email" "admin@example.com" --project Prometej_api
dotnet user-secrets set "Seed:Users:0:Password" "<password>" --project Prometej_api
dotnet user-secrets set "Seed:Users:0:FirstName" "Admin" --project Prometej_api
dotnet user-secrets set "Seed:Users:0:LastName" "Prometej" --project Prometej_api
dotnet user-secrets set "Seed:Users:0:Role" "admin" --project Prometej_api
```

Registration only creates students. An admin can make any of them a teacher under "Korisnici";
a seeded teacher account is a shortcut: repeat the five `Seed:Users` lines with index `1` and
role `teacher`.

### 2. The server

```
cd backend
dotnet run --project Prometej_api --launch-profile https
```

In development the server creates the database if it is missing, applies the migrations,
creates the seeded accounts and fills the periods with sample content and three quizzes. The
database user therefore has to be allowed to create a database and the `unaccent` extension.
The server listens on `https://localhost:7041`.

### 3. The client

```
cd frontend
npm install
npm run dev
```

It serves `http://localhost:5173` and forwards `/api` to the server, so start the server first.
`VITE_PROXY_TARGET` in a `.env` file changes the target (see `.env.example`).

### 4. The tutor (optional)

Without it the application works as before and shows no tutor. It needs
[Python 3.13](https://www.python.org/) and a deployment of GPT-4.1 on Azure OpenAI.

Copy `.env.example` in the repository's root to `.env` and fill in the endpoint, the key, the
deployment's name and the API version. The service reads the material from the server over
HTTPS, so it has to trust the server's development certificate. Export it, and tell the
server where the service listens:

```
cd backend
dotnet dev-certs https --export-path ../ai/.certs/dev.pem --format Pem --no-password
dotnet user-secrets set "Tutor:BaseUrl" "http://127.0.0.1:8000" --project Prometej_api
```

Set `PROMETEJ_API_CA_FILE=ai/.certs/dev.pem` in `.env`, delete the `dev.key` file the export
wrote beside the certificate, then:

```
cd ai
py -3.13 -m venv .venv
.venv\Scripts\python -m pip install -e ".[dev]"
.venv\Scripts\python -m uvicorn tutor.app:app --host 127.0.0.1 --port 8000
```

Start the server before the service, and restart the server after setting `Tutor:BaseUrl`.

## Tests

```
cd backend
dotnet test
```

The tests are integration tests: they start the real API against PostgreSQL in a container
and talk to it over HTTP, so Docker has to be running. They cover authentication and
authorization per role, quiz validation, the three question types and source texts, plays and
results, tests and sittings (what a sitting sends, deadlines, two requests ending one sitting),
both searches, the discussion, what the API forwards to the tutor and what it refuses first
(the tutor's service is a stub there), and the migrations (a migration is run against rows of the older
schema, to show what it does to them).

The client has no automated tests yet; `npm run lint` and `npm run build` are its gates.

The tutor's tests need no key and no network: the model is a scripted one and the material
two small texts.

```
cd ai
.venv\Scripts\python -m pytest
```

They cover the parsing of a text into sections (against a seeded text as well), the quote
check, the loop (a wrong quote sent back once, two wrong quotes never shown, the limit on
tool calls), the service's answers and its log line, and the scoring of the eval. The eval
itself is not a test: `python -m evals.run --set coverage` asks the real model, costs money
and writes a result file.

## Sample content

Every period has study material in the same seven chapters: the name and time frame, the
social and historical context, the features of the period, European literature, Croatian
literature, the works, and a summary. Under "the works", the two to five works a student has to
know are taken one by one: a note on the writer, the literary elements, a short summary and the
characters. Three periods also have a sample quiz of thirteen questions: three on a poem, or
an excerpt of one, shown beside them, eight four-option questions, one matching and one
ordering question. The poems (by Hanibal Lucić, Silvije Strahimir Kranjčević and Antun Gustav
Matoš) are in the public domain and are quoted from Croatian Wikisource.

The choice of works follows the NCVVO exam catalogue. The facts were taken from Hrvatska
enciklopedija, lektire.hr, Croatian Wikipedia and Leksikon Marina Držića; the text was written
for this project. It is sample material and **has not been reviewed by a teacher**.

## Not done yet

- The application is not deployed.
- No password reset: a forgotten password cannot be recovered.
- No automated tests for the client.
- A test stops what the server can stop. It does not stop a student from looking an answer
  up, or from sitting it again from a second account. There are no grades, no classes and no
  list of who has not started.
- A mock sitting of a public quiz hides nothing: the same quiz can be opened as practice.
- In a discussion a post cannot be edited or reported; an admin deleting it is the only
  remedy, and nothing updates live.
- The tutor's quotes are checked against the material; its own sentences around them are
  not. It is measured with one model, on small sets, in one run.
- The tutor does not know about tests: a student sitting one can ask it.
- Anyone can ask the tutor, ten times a minute, and nothing caps what that costs. It is
  meant to run locally until a deployment sets a cap.
