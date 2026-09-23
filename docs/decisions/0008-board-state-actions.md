# ADR 0008: Explicit board state actions

Status: Accepted

Use a visible state selector on each board card for the initial board. This provides keyboard access and a clear persisted action without introducing drag-and-drop failure modes. The API changes state, sets the closure timestamp, and appends activity in one database save. Drag-and-drop can be layered on later without changing the work-item model.
