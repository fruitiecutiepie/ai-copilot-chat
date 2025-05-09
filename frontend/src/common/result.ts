/**
 * Result type for representing success or failure.
 * @template T - The type of the value on success.
 * @template E - The type of the error on failure.
 */
export type Result<T, E> =
  | { is_ok: true, ok: T }
  | { is_ok: false, err: E };

export const Ok = <T>(value: T): Result<T, never> => (
  { is_ok: true, ok: value }
);
export const Err = <E>(error: E): Result<never, E> => (
  { is_ok: false, err: error }
);
