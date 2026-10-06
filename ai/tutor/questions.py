"""The rules a four-option Question has to keep, as the Prometej API applies them.

The API is what refuses a Quiz (QuestionCreateRequest and QuizService.TrimAndCheck). The
rules are written once more here so a draft that would be refused is never offered, and
tests/fixtures/question_cases.json is run by both test suites so the two stay the same.
"""

MAX_TEXT = 500

OPTION_KEYS = ["firstAnswer", "secondAnswer", "thirdAnswer", "fourthAnswer"]


def problems(question: dict) -> list[str]:
    """Why the API would refuse this Question, in the shape `quiz/create` takes; empty if not."""
    found: list[str] = []

    title = question.get("questionTitle")
    if not isinstance(title, str) or not title.strip():
        found.append("The question has no title.")
    elif len(title) > MAX_TEXT:
        found.append(f"The title is longer than {MAX_TEXT} characters.")

    options = [question.get(key) for key in OPTION_KEYS]
    if any(not isinstance(option, str) or not option.strip() for option in options):
        found.append("A question needs four answers.")
    else:
        # Length is counted before trimming and sameness after it, as the API does.
        if any(len(option) > MAX_TEXT for option in options):
            found.append(f"An answer is longer than {MAX_TEXT} characters.")
        if len({option.strip() for option in options}) < len(options):
            found.append("The four answers must all differ.")

    correct = question.get("correctOption")
    if not isinstance(correct, int) or isinstance(correct, bool) or not 1 <= correct <= 4:
        found.append("The correct option is a number from 1 to 4.")

    return found
