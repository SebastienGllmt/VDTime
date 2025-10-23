export type DesktopAndTime = {
  Desktop: DesktopInfo;
  Time: TimeInfo;
};

export type TimeInfo = {
  Current: number;
  Total: number;
};

export type DesktopInfo = {
  Name: string;
  Id: string; // GUID
};
