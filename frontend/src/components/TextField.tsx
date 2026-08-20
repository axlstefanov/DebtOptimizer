interface TextFieldProps {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  numeric?: boolean;
  date?: boolean;
  optionalNote?: string;
  needsInput?: boolean;
  error?: string;
}

export function TextField({
  id,
  label,
  value,
  onChange,
  placeholder,
  numeric = false,
  date = false,
  optionalNote,
  needsInput = false,
  error
}: TextFieldProps) {
  return (
    <div className={needsInput ? "field needs-input" : "field"}>
      <label htmlFor={id}>
        <span>{label}</span>
        {needsInput && <span className="needs-input-tag">needs input</span>}
        {!needsInput && optionalNote && <span className="needs-input-tag">{optionalNote}</span>}
      </label>
      <input
        id={id}
        type={date ? "date" : "text"}
        className={numeric ? "numeric" : undefined}
        inputMode={numeric ? "decimal" : undefined}
        autoComplete="off"
        value={value}
        placeholder={placeholder}
        aria-invalid={error ? true : undefined}
        onChange={(event) => onChange(event.target.value)}
      />
      {error && <span className="field-error">{error}</span>}
    </div>
  );
}
