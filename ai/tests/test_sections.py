from pathlib import Path

from tutor.sections import chapter_text, find, parse

SEED = Path(__file__).resolve().parents[2] / "backend/Prometej_api/Seed/Content/periods"

DOCUMENT = """
<h1>Naslov</h1>
<p>Uvod prije prvog poglavlja.</p>
<h2>Prvo</h2>
<p>Tekst  prvog&nbsp;poglavlja.</p>
<h2><br></h2>
<h3>Dio</h3>
<h4>Likovi</h4>
<ul><li><strong>Lovro</strong> je darovit<br>mladić.</li></ul>
<h2>Drugo</h2>
<h3>Djelo</h3>
<p>Tekst djela.</p>
<blockquote><p>Citat u djelu.</p></blockquote>
"""


def test_an_empty_heading_is_counted_and_gets_no_section():
    sections = parse(DOCUMENT)

    assert [(section.id, section.level, section.title) for section in sections] == [
        ("odjeljak-0", 2, "Prvo"),
        ("odjeljak-2", 3, "Dio"),
        ("odjeljak-3", 2, "Drugo"),
        ("odjeljak-4", 3, "Djelo"),
    ]


def test_a_sections_text_is_its_blocks_cleaned_as_the_search_cleans_them():
    sections = parse(DOCUMENT)

    assert find(sections, "odjeljak-0").text == "Tekst prvog poglavlja."
    # A smaller heading is text of the part it is under, and a line break reads as a space.
    assert find(sections, "odjeljak-2").text == "Likovi\nLovro je darovit mladić."
    assert find(sections, "odjeljak-4").text == "Tekst djela.\nCitat u djelu."


def test_text_before_the_first_chapter_is_in_no_section():
    assert all("Uvod" not in section.text for section in parse(DOCUMENT))


def test_a_part_names_its_chapter():
    sections = parse(DOCUMENT)

    assert find(sections, "odjeljak-4").parent_id == "odjeljak-3"
    assert find(sections, "odjeljak-3").parent_id is None


def test_reading_a_chapter_returns_its_parts_and_reading_a_part_only_itself():
    sections = parse(DOCUMENT)

    assert chapter_text(sections, "odjeljak-3") == "Djelo\nTekst djela.\nCitat u djelu."
    assert chapter_text(sections, "odjeljak-4") == "Tekst djela.\nCitat u djelu."
    assert chapter_text(sections, "odjeljak-9") is None


def test_a_seeded_text_parses_into_the_sections_the_period_page_shows():
    html = (SEED / "4-realizam.html").read_text(encoding="utf-8")
    sections = parse(html)

    assert [section.title for section in sections if section.level == 2] == [
        "Naziv i vremenski okvir",
        "Društveni i povijesni kontekst",
        "Obilježja razdoblja",
        "Europska književnost",
        "Hrvatska književnost",
        "Djela",
        "Sažetak",
    ]
    # No heading of the seeded texts is empty, so an id is the heading's place among them.
    assert [section.id for section in sections] == [
        f"odjeljak-{index}" for index in range(len(sections))
    ]

    work = next(section for section in sections if "Zločin i kazna" in section.title)
    assert work.title == "Fjodor Mihajlovič Dostojevski: Zločin i kazna"
    assert work.level == 3
    assert find(sections, work.parent_id).title == "Djela"
    assert "Rodion Romanovič Raskoljnikov bivši je student koji živi u bijedi." in work.text
