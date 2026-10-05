export interface ContentsEntry {
  id: string;
  text: string;
  level: 2 | 3;
}

// The ids are made here and not stored in the content: the editor drops attributes, and an
// Admin's text has to get a contents list as well.
export function withHeadingIds(html: string): {
  html: string;
  entries: ContentsEntry[];
} {
  const body = new DOMParser().parseFromString(html, "text/html").body;
  const entries: ContentsEntry[] = [];
  body.querySelectorAll("h2, h3").forEach((heading, index) => {
    const text = heading.textContent?.trim() ?? "";
    if (text === "") return;
    heading.id = `odjeljak-${index}`;
    entries.push({
      id: heading.id,
      text,
      level: heading.tagName === "H2" ? 2 : 3,
    });
  });
  return { html: body.innerHTML, entries };
}
