import { useEffect, useRef, useState } from "react";
import { Typography } from "@mui/material";

interface CountdownProps {
  // when the server ends the Sitting
  endsAt: string;
  // the server's clock minus the browser's, in milliseconds
  offset: number;
  onZero: () => void;
}

const twoDigits = (value: number) => String(value).padStart(2, "0");

// The time left by the server's clock. It only shows it: the server ends the Sitting.
export default function Countdown({ endsAt, offset, onZero }: CountdownProps) {
  const [now, setNow] = useState(() => Date.now() + offset);
  const zeroReached = useRef(false);

  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now() + offset), 1000);
    return () => clearInterval(timer);
  }, [offset]);

  const secondsLeft = Math.max(
    0,
    Math.ceil((new Date(endsAt).getTime() - now) / 1000)
  );

  useEffect(() => {
    if (secondsLeft === 0 && !zeroReached.current) {
      zeroReached.current = true;
      onZero();
    }
  }, [secondsLeft, onZero]);

  return (
    <Typography
      className={`sitting-clock${secondsLeft < 60 ? " ending" : ""}`}
      role="timer"
    >
      Preostalo: {twoDigits(Math.floor(secondsLeft / 60))}:
      {twoDigits(secondsLeft % 60)}
    </Typography>
  );
}
