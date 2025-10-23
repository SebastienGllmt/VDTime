import net from "node:net";
import readline from "node:readline";

export class PipeClient {
  private socket: net.Socket;
  private rl: readline.Interface;
  private pending: Array<(line: string) => void> = [];

  constructor(pipePath: string, onClose: () => void) {
    this.socket = net.createConnection(pipePath);
    this.socket.setEncoding("utf8");
    this.rl = readline.createInterface({ input: this.socket });

    this.rl.on("line", (rawLine) => {
      let line = rawLine.replace(/^\uFEFF/, ""); // remove BOM

      // note: assumes responses come in order (not guaranteed in general, but works for vdtime)
      const resolver = this.pending.shift();
      if (!resolver) return; // should never happen that we get a response with no corresponding input
      resolver(line);
    });

    const failAll = (err: unknown) => {
      // handle socket error: reject all pending
      while (this.pending.length) {
        const r = this.pending.shift();
        if (r) r(Promise.reject(err) as any);
      }
    };
    this.socket.on("error", failAll);
    this.socket.on("close", () => {
      onClose();
      failAll(new Error("pipe closed"));
      this.close();
    });
  }

  sendCommand(cmd: string, timeoutMs = 5000): Promise<string> {
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        reject(new Error("Timeout waiting for response"));
      }, timeoutMs);

      const request = (line: string) => {
        clearTimeout(timer);
        resolve(line);
      };
      this.pending.push(request);

      const handleError = (err: unknown) => {
        clearTimeout(timer);
        // remove the resolver we just added
        const idx = this.pending.indexOf(request);
        if (idx >= 0) this.pending.splice(idx, 1);
        reject(err);
      };

      try {
        this.socket.write(cmd + "\n", "utf8", (err) => {
          if (err) handleError(err);
        });
      } catch (err) {
        handleError(err);
      }
    });
  }

  close() {
    try {
      this.rl.close();
    } catch (err) {}
    try {
      this.socket.end();
    } catch (err) {}
  }
}
