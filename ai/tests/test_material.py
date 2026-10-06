import httpx

from tests.conftest import REALIZAM
from tutor.material import ApiMaterial

SEARCH = [
    {
        "periodId": 4,
        "periodName": "Realizam",
        "matchCount": 3,
        "passages": [
            {"heading": "Kratak sadržaj", "text": "…Raskoljnikov bivši je student koji živi…"},
            {"heading": None, "text": "August Šenoa: Prijan Lovro"},
            {"heading": "Negdje", "text": "Rečenica koje u tekstu nema."},
        ],
    }
]


def material(seen: list[httpx.Request] | None = None) -> ApiMaterial:
    def handler(request: httpx.Request) -> httpx.Response:
        if seen is not None:
            seen.append(request)
        path = request.url.path
        if path == "/api/period":
            return httpx.Response(
                200, json=[{"id": 4, "name": "Realizam", "timeFrame": "1881. – 1892."}]
            )
        if path == "/api/period/content/4":
            return httpx.Response(200, json={"id": 1, "periodId": 4, "content": REALIZAM})
        if path == "/api/period/content/search":
            return httpx.Response(200, json=SEARCH)
        return httpx.Response(404)

    return ApiMaterial("https://prometej.test", transport=httpx.MockTransport(handler))


def test_the_periods_are_read_with_their_time_frame():
    assert [(p.id, p.name, p.time_frame) for p in material().periods()] == [
        (4, "Realizam", "1881. – 1892.")
    ]


def test_a_period_without_content_has_no_sections():
    assert material().sections(7) is None


def test_a_search_passage_is_given_the_section_it_was_cut_from():
    cut, heading, lost = material().search("raskoljnikov")

    assert (cut.period_id, cut.period_name) == (4, "Realizam")
    assert cut.section_id == "odjeljak-2"
    assert cut.section_title == "Fjodor Mihajlovič Dostojevski: Zločin i kazna"
    # A passage that is a heading is found by its title.
    assert heading.section_id == "odjeljak-3"
    # One found nowhere stays without a section.
    assert lost.section_id is None


def test_a_query_is_cut_to_what_the_search_accepts():
    seen: list[httpx.Request] = []

    material(seen).search("a" * 150)

    assert seen[0].url.params["query"] == "a" * 100
