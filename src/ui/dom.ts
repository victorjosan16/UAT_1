type Attrs = Record<string, string> & { className?: string; text?: string };

export function el<K extends keyof HTMLElementTagNameMap>(tag: K, attrs: Attrs = {}, children: (Node | string)[] = []): HTMLElementTagNameMap[K] {
  const node = document.createElement(tag);
  for (const [key, value] of Object.entries(attrs)) {
    if (key === "className") node.className = value;
    else if (key === "text") node.textContent = value;
    else node.setAttribute(key, value);
  }
  for (const child of children) {
    node.append(child instanceof Node ? child : document.createTextNode(child));
  }
  return node;
}

export function formatNumber(value: number): string {
  return Math.round(value).toLocaleString("en-US");
}
