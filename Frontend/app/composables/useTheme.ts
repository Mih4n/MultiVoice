export type Theme = "light" | "dark";

const STORAGE_KEY = "mp-theme";

function readSavedTheme(): Theme | null {
  if (typeof localStorage === "undefined") return null;
  const value = localStorage.getItem(STORAGE_KEY);
  return value === "dark" || value === "light" ? value : null;
}

function readSystemTheme(): Theme {
  if (typeof window === "undefined") return "light";
  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function useTheme() {
  const theme = useState<Theme>("site-theme", () => {
    if (import.meta.server) return "light";
    return readSavedTheme() ?? readSystemTheme();
  });

  const setTheme = (value: Theme) => {
    if (import.meta.server) return;
    localStorage.setItem(STORAGE_KEY, value);
    document.documentElement.setAttribute("data-theme", value);
    theme.value = value;
  };

  const toggleTheme = () => {
    setTheme(theme.value === "light" ? "dark" : "light");
  };

  return {
    theme,
    setTheme,
    toggleTheme,
  };
}
