# Prometej

Prometej is a web platform for learning Croatian literature. The study material is organised
by literary period, students practise it with quizzes in the forms the national exam (matura)
uses, and teachers can give a class a timed test and see where it went wrong. A built-in
tutor answers questions about the material and quotes the text it relied on.

I started it in 2024 as my final paper at university
([the paper](https://drive.google.com/file/d/1dKCoy6_ElFA_kVEj95mHxvKKT-1d5zTl/view?usp=drive_link),
in Croatian, describes that first version). Since then I have rebuilt most of it.

The interface is in Croatian. The app is not deployed yet, so for now it runs locally
(see [Getting started](#getting-started)).

## Contents

- [Features](#features)
- [Tech stack](#tech-stack)
- [Project structure](#project-structure)
- [Getting started](#getting-started)
- [Tests](#tests)
- [How it works](#how-it-works)
- [How the tutor is measured](#how-the-tutor-is-measured)
- [Sample content](#sample-content)

## Features

**Reading**

- Study material for the twelve periods of the exam catalogue, each with a contents list
  that follows you as you read.
- Search across all the material. It ignores case and Croatian diacritics, so "senoa"
  finds "Šenoa".
- A discussion under every period, with topics and replies.

**Practising**

- Three kinds of question: four options, matching pairs, and putting items in order.
- Questions about a poem or an excerpt, shown beside the text, which is how most of the
  exam's literature tasks are asked.
- An optional hint before answering and an explanation after it.
- Public quizzes that anyone can play without an account, and private ones opened with a
  five-digit code from the teacher.
- For signed-in students: every result is stored and can be reviewed answer by answer,
  with progress shown per period.

**Tests**

- A teacher can turn a private quiz into a test with an optional time limit and closing time.
- A student sits it once. No answer is shown before the end, answers are saved as they are
  given, and the server keeps the clock.
- Any public quiz can also be solved the same way, as a mock test.

**For teachers**

- A quiz editor that mixes all three question kinds and source texts.
- Analytics for each quiz: every play, how often each question was answered right, the
  most common wrong answer, and each student's first and best result.
- For a test: who has started, how each sitting stands, closing the test, and letting one
  student sit it again.

**For admins**

- A rich-text editor for the material of each period.
- Role management (making a student a teacher or an admin) and moderation of discussions.

**The tutor ("Prometej")**

- Answers questions from the study material only. Every answer names the sections it rests
  on and quotes them, and a click opens the period at that heading.
- Says so when the material does not cover a question, and declines to write essays or
  homework.
- For teachers, it drafts four-option questions from a chosen chapter, each shown with the
  sentence that supports its right answer. Nothing is saved until the teacher saves the quiz.

## Tech stack

| Part | Technology |
| --- | --- |
| API | .NET 8, ASP.NET Core, EF Core, PostgreSQL |
| Client | React 18, TypeScript, Vite, Redux Toolkit, MUI |
| Tutor | Python 3.13, FastAPI, GPT-4.1 on Azure OpenAI |
| Tests | xUnit with PostgreSQL in a container, pytest |

## Project structure

```
backend/
  Prometej_api/            host, controllers, authentication, seed content
  Prometej_core/           entities, request and view models, services
  Prometej_persistance/    DbContext, migrations, repository
  Prometej_tests/          integration tests
frontend/                  the React client
ai/
  tutor/                   prompts, tools, the answer loop, the quote check, question drafts
  evals/                   evaluation sets, pass bars, the runner and every recorded result
  tests/                   pytest, against a scripted model
```

The browser only talks to the .NET API. Requests for the tutor are validated and rate
limited there and then forwarded to the Python service, which reads the study material back
through the API's public endpoints.

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/)
- PostgreSQL
- Docker, only for running the backend tests
- [Python 3.13](https://www.python.org/) and an Azure OpenAI deployment of GPT-4.1, only
  for the tutor

### 1. Configure the API

No secret is kept in a tracked file. From `backend/`, set the connection string, a signing
key for the session token (base64 of at least 32 random bytes, for example the output of
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

Registering in the app always creates a student. An admin can promote any account under
"Korisnici". To have a teacher account from the start, repeat the five `Seed:Users` lines
with index `1` and the role `teacher`.

### 2. Run the API

```
cd backend
dotnet run --project Prometej_api --launch-profile https
```

In development the API creates the database if it is missing, applies the migrations,
creates the seeded accounts and fills the periods with sample content and three quizzes.
The database user therefore needs the right to create a database and the `unaccent`
extension. The API listens on `https://localhost:7041`.

### 3. Run the client

```
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`. The dev server forwards `/api` to the API, so start the API
first. To point it somewhere else, set `VITE_PROXY_TARGET` in `frontend/.env`
(see `frontend/.env.example`).

### 4. Run the tutor (optional)

Without the tutor the app works as usual and simply does not show it.

Copy `.env.example` in the repository root to `.env` and fill in the Azure OpenAI endpoint,
key, deployment name and API version. The service reads the material from the API over
HTTPS, so it has to trust the API's development certificate. Export the certificate and
tell the API where the service listens:

```
cd backend
dotnet dev-certs https --export-path ../ai/.certs/dev.pem --format Pem --no-password
dotnet user-secrets set "Tutor:BaseUrl" "http://127.0.0.1:8000" --project Prometej_api
```

Set `PROMETEJ_API_CA_FILE=ai/.certs/dev.pem` in `.env` and delete the `dev.key` file the
export wrote next to the certificate. Then:

```
cd ai
py -3.13 -m venv .venv
.venv\Scripts\python -m pip install -e ".[dev]"
.venv\Scripts\python -m uvicorn tutor.app:app --host 127.0.0.1 --port 8000
```

Start the API before the service, and restart the API after setting `Tutor:BaseUrl`.

## Tests

```
cd backend
dotnet test
```

The backend tests are integration tests. They start the real API against PostgreSQL in a
container and call it over HTTP, so Docker has to be running. They cover authentication
and authorization per role, quiz validation, the three question kinds, plays and results,
tests and sittings, both searches, the discussion, what the API forwards to the tutor, and
the data migrations.

```
cd ai
.venv\Scripts\python -m pytest
```

The tutor's tests need no key and no network, because the model is a scripted one. They
cover parsing a text into sections, the quote check, the answer loop, question drafts and
the scoring of the evaluation.

The client has no automated tests yet. `npm run lint` and `npm run build` are its checks.

## How it works

A few decisions that explain most of the code:

- **Sessions.** The session is a JWT in an `HttpOnly`, `Secure`, `SameSite=Lax` cookie that
  the page's script never sees. The token only says who is calling. The role, and whether
  the session is still valid, are read from the account on every request, so a role change
  or a password change takes effect at once.
- **The server computes the result.** A play is submitted as numbers (the option chosen,
  the pairs made, the order given). The server scores it from the stored questions and
  ignores any score sent by the client.
- **Old results never change.** An answer keeps the texts it was played with, and a question
  removed from a played quiz is retired instead of deleted. Editing a quiz does not rescore
  earlier plays.
- **One answer row per point.** A four-option question stores one row, a matching question
  one per pair, an ordering question one per place. The score is the number of right rows.
- **A test is sat on the server.** A test's questions reach a student only inside a sitting
  and without their answers. The server shuffles the lists, saves each answer as it is
  given and ends a sitting whose time has run out.
- **Typed errors.** Services throw exceptions such as `NotFoundException` or
  `ForbiddenException`, and one handler maps them to status codes. Controllers have no
  `try/catch`.
- **Rate limits.** Requests that carry an entry code and questions to the tutor are limited
  per account, or per address when nobody is signed in. Posts in a discussion are limited
  per account.
- **The tutor quotes the material or does not answer.** The model reads the texts through
  three tools (search, list a period's sections, read a section). Every quote in its answer
  is looked up word for word in the section it names before anything is shown. A failed
  answer goes back to the model once. After a second failure the student gets a fixed
  message instead of an answer.

## How the tutor is measured

I wrote the pass bars down before the first run. The commit that holds them
(`ai/evals/bars.json`, together with the sets) is older than the one that holds the results.
A script scores every run and no model judges another.

| Set | Items | Bar | Result |
| --- | --- | --- | --- |
| A. Quiz questions, with tools | 24 | at least 90% | 24 (100%) |
| A. The same questions, no tools (baseline) | 24 | | 20 (83.3%) |
| A. What the tools add | | at least 10 points | +16.7 points |
| B. "Where is this covered?" | 36 | at least 85% | 36 (100%) |
| C. Not covered, or homework | 15 | at least 90% | 15 (100%) |

- **Set A** is the 24 four-option questions of the sample quizzes that are not about a
  poem. The tutor gets the question with its options and has to pick one, through the same
  loop and quote check as in the app. The baseline is the same model with no tools and no
  material.
- **Set B** asks about something without naming the period. An item passes when a checked
  citation is in the expected period and section.
- **Set C** is ten questions about works the material does not cover and five requests for
  an essay or homework. An item passes when the tutor does not answer.

One run of each set on 6 October 2026, with GPT-4.1 at temperature 0. Two answers in set A
needed the one retry, and none failed the quote check twice. The four runs used about
487,000 tokens, and a typical answer took about five seconds.

Question drafts had their own bars, also committed before their run. Five drafts were
requested from the first work of each of the twelve periods:

| Check | Of | Bar | Result |
| --- | --- | --- | --- |
| The app's own rules accept the draft | 60 | at least 95% | 60 (100%) |
| The supporting quote is in the section | 60 | at least 95% | 59 (98.3%) |
| A second reading picks the same answer | 59 | at least 85% | 59 (100%) |
| Rated usable by a person | 30 | at least 70% | 30 (100%) |

What these numbers do not show:

- The quote is checked, the tutor's own wording around it is not. Set A is the only measure
  of whether an explanation is right, and it has 24 questions.
- The sets are small, and I wrote them as well as the prompt. Three results of 100% mostly
  mean that these sets are too easy to find where the tutor fails.
- It is one model and one run. A second run could differ by an item or two.
- Nothing here measures a conversation with follow-up questions, or a student trying to
  talk the tutor out of its rules.
- The drafts were rated by one person, me. I built the feature and I am not a teacher.

`ai/evals/results` holds every recorded run, item by item.

## Sample content

Every period comes with study material in the same seven chapters: name and time frame,
social and historical context, features of the period, European literature, Croatian
literature, the works, and a summary. Three periods (Renesansa, Realizam, Modernizam) also
have a sample quiz of thirteen questions.

The choice of works follows the NCVVO exam catalogue. The facts come from Hrvatska
enciklopedija, lektire.hr, Croatian Wikipedia and Leksikon Marina Držića, and the text was
written for this project. The poems used in the quizzes (by Hanibal Lucić, Silvije Strahimir
Kranjčević and Antun Gustav Matoš) are in the public domain and are quoted from Croatian
Wikisource.

This is sample material and **it has not been reviewed by a teacher**.
