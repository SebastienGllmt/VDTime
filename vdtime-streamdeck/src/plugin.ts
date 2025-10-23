import streamDeck, { LogLevel } from "@elgato/streamdeck";

import { CurrentTimeTracker, AllTimeTracker } from "./actions/time-tracker";

streamDeck.logger.setLevel(LogLevel.INFO);

streamDeck.actions.registerAction(new CurrentTimeTracker());
streamDeck.actions.registerAction(new AllTimeTracker());

// Finally, connect to the Stream Deck.
streamDeck.connect();
