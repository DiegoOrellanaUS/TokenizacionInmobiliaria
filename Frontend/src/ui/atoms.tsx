import type { ButtonHTMLAttributes, ReactNode } from 'react'

export function Button({ variant = 'primary', className = '', ...props }:
  ButtonHTMLAttributes<HTMLButtonElement> & { variant?: 'primary' | 'secondary' | 'danger' }) {
  return <button className={`button button-${variant} ${className}`} {...props} />
}

const nombres: Record<string, string> = {
  BORRADOR: 'Borrador', EN_REVISION: 'En revisión', CON_OBSERVACIONES: 'Con observaciones',
  APROBADO: 'Aprobado', CONFIRMADA: 'Confirmada', FALLIDA: 'Fallida', CANCELADA: 'Cancelada', PENDIENTE: 'Pendiente',
  ACTIVA: 'Activa', APROVISIONANDO: 'Preparando espacio', LISTO: 'Listo', PUBLICADA: 'Publicada',
}

export function Estado({ value }: { value: string }) {
  return <span className="status" data-value={value}>{nombres[value] || value}</span>
}

export function Campo({ label, children }: { label: string; children: ReactNode }) {
  return <label className="field"><span>{label}</span>{children}</label>
}

export function ErrorBox({ text }: { text?: string }) {
  return text ? <p className="alert" role="alert">{text}</p> : null
}
