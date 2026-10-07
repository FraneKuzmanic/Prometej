import { MouseEvent, useEffect, useRef, useState } from "react";
import { useMediaQuery } from "@mui/material";
import { ContentsEntry } from "./headings";
import "./styles.css";

// A heading counts as reached once it is this far below the top of the scrolling area:
// under the fixed header, and a little below where a click on an entry puts it.
const READING_LINE = 100;

interface ContentsListProps {
  entries: ContentsEntry[];
  // told which entry was chosen, for one that has to be opened before it can be read
  onGoTo?: (id: string) => void;
}

export default function ContentsList({ entries, onGoTo }: ContentsListProps) {
  const wide = useMediaQuery("(min-width:1200px)");
  const [current, setCurrent] = useState<string>();
  const details = useRef<HTMLDetailsElement>(null);
  const nav = useRef<HTMLElement>(null);

  useEffect(() => {
    // The page does not scroll, the layout's <main> does.
    const main = document.querySelector("main");
    if (!main || entries.length === 0) return;

    const findCurrent = () => {
      const line = main.getBoundingClientRect().top + READING_LINE;
      let reached = entries[0].id;
      for (const entry of entries) {
        const heading = document.getElementById(entry.id);
        if (heading && heading.getBoundingClientRect().top <= line) {
          reached = entry.id;
        }
      }
      // The last chapters can be too short to ever reach the line.
      const atEnd =
        main.scrollTop > 0 &&
        main.scrollTop + main.clientHeight >= main.scrollHeight - 2;
      setCurrent(atEnd ? entries[entries.length - 1].id : reached);
    };

    findCurrent();
    main.addEventListener("scroll", findCurrent, { passive: true });
    return () => main.removeEventListener("scroll", findCurrent);
  }, [entries]);

  // A long list scrolls inside itself; keep the entry being read in sight.
  useEffect(() => {
    const list = nav.current;
    const entry = list?.querySelector<HTMLElement>('[aria-current="location"]');
    if (!list || !entry) return;
    const top =
      entry.getBoundingClientRect().top - list.getBoundingClientRect().top + list.scrollTop;
    if (top < list.scrollTop || top + entry.offsetHeight > list.scrollTop + list.clientHeight) {
      list.scrollTop = top - list.clientHeight / 2;
    }
  }, [current]);

  if (entries.length < 2) return null;

  const goTo = (event: MouseEvent, id: string) => {
    event.preventDefault();
    const reducedMotion = window.matchMedia(
      "(prefers-reduced-motion: reduce)"
    ).matches;
    // The folded list closes first: closing it moves the text, and the scroll has to
    // aim at where the heading is afterwards.
    if (details.current) details.current.open = false;
    onGoTo?.(id);
    requestAnimationFrame(() =>
      document
        .getElementById(id)
        ?.scrollIntoView({ behavior: reducedMotion ? "auto" : "smooth", block: "start" })
    );
  };

  const link = (entry: ContentsEntry) => (
    <a
      href={`#${entry.id}`}
      aria-current={entry.id === current ? "location" : undefined}
      onClick={(event) => goTo(event, entry.id)}
    >
      {entry.text}
    </a>
  );

  // Each chapter with the headings one level below it.
  const chapters: { entry: ContentsEntry; parts: ContentsEntry[] }[] = [];
  entries.forEach((entry) => {
    if (entry.level === 2 || chapters.length === 0) {
      chapters.push({ entry, parts: [] });
    } else {
      chapters[chapters.length - 1].parts.push(entry);
    }
  });

  const list = (
    <ol>
      {chapters.map(({ entry, parts }) => (
        <li key={entry.id}>
          {link(entry)}
          {parts.length > 0 && (
            <ol>
              {parts.map((part) => (
                <li key={part.id}>{link(part)}</li>
              ))}
            </ol>
          )}
        </li>
      ))}
    </ol>
  );

  return (
    <nav className="contents-list" aria-label="Sadržaj" ref={nav}>
      {wide ? (
        <>
          <p className="contents-list-title">Sadržaj</p>
          {list}
        </>
      ) : (
        <details ref={details}>
          <summary>Sadržaj</summary>
          {list}
        </details>
      )}
    </nav>
  );
}
