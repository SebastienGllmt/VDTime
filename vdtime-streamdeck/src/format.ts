import { DesktopAndTime } from "./model";

function toLineCurrent(data: DesktopAndTime, longestName: number) {
  return `${padName(data.Desktop.Name, longestName)}\n${formatSeconds(
    data.Time.Current
  )}`;
}
function toLineTotal(data: DesktopAndTime, longestName: number) {
  return `${padName(data.Desktop.Name, longestName)}\n${formatSeconds(
    data.Time.Total
  )}`;
}

export function formatTimeAll(info: DesktopAndTime[]): string {
  return formatTimeBase(info, toLineTotal);
}
export function formatTimeCurrent(info: DesktopAndTime[]): string {
  const current = info.filter((info) => info.Time.Current > 0)[0];
  return formatTimeBase([current], toLineCurrent);
}
export function formatTimeBase(
  info: DesktopAndTime[],
  toLine: (data: DesktopAndTime, len: number) => string
): string {
  const longestName = Math.max(...info.map((data) => data.Desktop.Name.length));
  return info.map((desktop) => toLine(desktop, longestName)).join("\n\n");
}

function padName(name: string, len: number): string {
  if (name.length == len) return name;
  return " ".repeat(len - name.length) + name;
}
function formatSeconds(sec: number) {
  const s = Math.floor(sec % 60);
  const m = Math.floor((sec / 60) % 60);
  const h = Math.floor(sec / 3600);

  function pad(num: number, padChar: string): string {
    if (num < 10) {
      return `${padChar}${num}`;
    }
    return num.toString();
  }

  if (h > 0) return `${pad(h, " ")}h${pad(m, "0")}`;
  if (m > 0) return `${pad(m, " ")}m${pad(s, "0")}`;
  return `  ${s}s`;
}
