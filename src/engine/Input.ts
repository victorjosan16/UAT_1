export type InputHandler = () => void;

/**
 * Normalizes tap / click / Space into a single `place` action. Mobile-first:
 * pointerdown fires the action immediately for minimum latency, and default
 * touch behaviors (scroll/zoom/double-tap-zoom) are suppressed on the
 * target element.
 */
export class InputController {
  private handler: InputHandler | null = null;
  private readonly target: HTMLElement;
  private enabled = true;

  private readonly onPointerDown = (event: PointerEvent) => {
    if (!this.enabled) return;
    event.preventDefault();
    this.handler?.();
  };

  private readonly onKeyDown = (event: KeyboardEvent) => {
    if (!this.enabled) return;
    if (event.code === "Space" || event.key === " ") {
      event.preventDefault();
      this.handler?.();
    }
  };

  private readonly onContextMenu = (event: Event) => {
    event.preventDefault();
  };

  constructor(target: HTMLElement) {
    this.target = target;
    this.target.addEventListener("pointerdown", this.onPointerDown, { passive: false });
    window.addEventListener("keydown", this.onKeyDown);
    this.target.addEventListener("contextmenu", this.onContextMenu);
  }

  onPlace(handler: InputHandler): void {
    this.handler = handler;
  }

  setEnabled(enabled: boolean): void {
    this.enabled = enabled;
  }

  destroy(): void {
    this.target.removeEventListener("pointerdown", this.onPointerDown);
    window.removeEventListener("keydown", this.onKeyDown);
    this.target.removeEventListener("contextmenu", this.onContextMenu);
  }
}
