import streamDeck, {
  action,
  ApplicationDidLaunchEvent,
  ApplicationDidTerminateEvent,
  KeyDownEvent,
  SingletonAction,
  WillAppearEvent,
} from "@elgato/streamdeck";
import { PipeClient } from "../ipc";
import { DesktopAndTime } from "../model";
import { formatTimeAll, formatTimeCurrent } from "../format";

const PIPE_PATH = `\\\\.\\pipe\\vdtime-pipe`; // note the escaping

let pollInterval: NodeJS.Timeout | null = null;
let client: PipeClient | null = null;

streamDeck.system.onApplicationDidLaunch((ev: ApplicationDidLaunchEvent) => {
  streamDeck.logger.info(`Launch: ${ev.application}`);

  client = new PipeClient(PIPE_PATH);

  pollInterval = setInterval(async () => {
    try {
      if (client == null) return;
      const json = await client.sendCommand("time_all");
      const reply: DesktopAndTime[] = JSON.parse(json);
      streamDeck.logger.trace("reply:", reply);
      streamDeck.actions.forEach((action) => {
        if (action.manifestId.includes("alltracker")) {
          action.setTitle(formatTimeAll(reply));
        } else {
          action.setTitle(formatTimeCurrent(reply));
        }
      });
    } catch (err) {
      streamDeck.logger.error("error:", err);
    }
  }, 1000);
});
streamDeck.system.onApplicationDidTerminate(
  (ev: ApplicationDidTerminateEvent) => {
    if (pollInterval) {
      clearInterval(pollInterval);
      pollInterval = null;
      client = null;
      streamDeck.actions.forEach((action) => action.showAlert());
      streamDeck.logger.info(`Terminate: ${ev.application}`);
    }
  }
);

@action({ UUID: "com.sebastiengllmt.vdtime.alltracker" })
export class AllTimeTracker extends SingletonAction<AllTimeTrackerSettings> {
  override onWillAppear(
    ev: WillAppearEvent<AllTimeTrackerSettings>
  ): void | Promise<void> {
    return ev.action.setTitle("Linking..");
  }
  override async onKeyDown(
    ev: KeyDownEvent<AllTimeTrackerSettings>
  ): Promise<void> {
    if (client != null) await client.sendCommand("reset");
  }
}
type AllTimeTrackerSettings = {};

@action({ UUID: "com.sebastiengllmt.vdtime.currenttracker" })
export class CurrentTimeTracker extends SingletonAction<CurrentTimeTrackerSettings> {
  override onWillAppear(
    ev: WillAppearEvent<CurrentTimeTrackerSettings>
  ): void | Promise<void> {
    return ev.action.setTitle("Linking..");
  }
}
type CurrentTimeTrackerSettings = {};
