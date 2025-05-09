import { Err, Ok, Result } from "./result";

// Asynchronous
export async function resAsync<T>(
  fn: () => Promise<T>
): Promise<Result<T, unknown>> {
  try {
    return Ok(await fn());
  } catch (err: unknown) {
    return Err(err);
  }
}

// Synchronous function overload
export function res<T>(
  fn: () => T
): Result<T, unknown> {
  try {
    return Ok(fn());
  } catch (err: unknown) {
    return Err(err);
  }
}
