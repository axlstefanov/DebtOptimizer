const amountFormat = new Intl.NumberFormat(undefined, {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2
});

const rateFormat = new Intl.NumberFormat(undefined, {
  minimumFractionDigits: 0,
  maximumFractionDigits: 2
});

const dateFormat = new Intl.DateTimeFormat(undefined, {
  year: "numeric",
  month: "short"
});

export const formatAmount = (value: number) => amountFormat.format(value);

export const formatRate = (value: number) => `${rateFormat.format(value)}%`;

export function formatMonth(isoDate: string): string {
  const [year, month] = isoDate.split("-").map(Number);
  return dateFormat.format(new Date(year, month - 1, 1));
}

export function toDecimal(raw: string): number | null {
  const trimmed = raw.trim().replace(/,/g, "");
  if (trimmed === "") return null;

  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : null;
}

export const fromNumber = (value: number | null | undefined) =>
  value === null || value === undefined ? "" : String(value);
