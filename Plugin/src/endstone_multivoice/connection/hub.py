import threading
import time

from signalrcore.hub_connection_builder import HubConnectionBuilder


class Hub:
    """One SignalR connection to MinecraftHub, reconnected forever from a background thread."""

    def __init__(self, url: str, secret: str, logger, handlers: dict, on_open) -> None:
        self.url = url
        self.secret = secret
        self.logger = logger
        self.handlers = handlers
        self.on_open = on_open
        self.connection = None
        self.connected = False
        self.running = False

    def start(self) -> None:
        self.running = True
        threading.Thread(target=self.connect_loop, daemon=True).start()

    def stop(self) -> None:
        self.running = False
        if self.connection is not None:
            self.connection.stop()

    def send(self, method: str, *args) -> bool:
        # dropped while disconnected; the full state is resent after reconnecting
        if not self.connected:
            return False
        self.connection.send(method, list(args))
        return True

    def opened(self) -> None:
        self.connected = True
        self.on_open()
        self.logger.info("Connected to MultiVoice server")

    def closed(self) -> None:
        self.connected = False

    def connect_loop(self) -> None:
        while self.running:
            if not self.connected:
                if self.connection is not None:
                    self.connection.stop()
                self.connection = HubConnectionBuilder() \
                    .with_url(self.url, options={"headers": {"X-Secret": self.secret}}) \
                    .build()
                self.connection.on_open(self.opened)
                self.connection.on_close(self.closed)
                for method, handler in self.handlers.items():
                    self.connection.on(method, handler)
                try:
                    self.connection.start()
                except Exception as e:
                    self.logger.warning(f"MultiVoice connection failed: {e}")
            time.sleep(3)
