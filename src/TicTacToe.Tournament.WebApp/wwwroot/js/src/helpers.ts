// helpers.ts

/**
* Starts the spinner animation on a button and disables it.
* @param buttonId The id of the button.
*/
export function startSpinner(buttonId: string): void {
    const btn = document.getElementById(buttonId);
    btn?.querySelector('i')?.classList.add('fa-spin');
    btn?.setAttribute('disabled', 'true');
}

/**
 * Stops the spinner animation on a button and enables it again.
 * @param buttonId The id of the button.
 */
export function stopSpinner(buttonId: string): void {
    const btn = document.getElementById(buttonId);
    btn?.querySelector('i')?.classList.remove('fa-spin');
    btn?.removeAttribute('disabled');
}

/**
 * Fetches JSON from a given URL safely, throwing an error if it fails.
 * @param url The endpoint to fetch.
 * @param options Request options.
 * @returns The parsed JSON object.
 */
export async function safeFetchJson<T>(url: string, options?: RequestInit): Promise<T> {
    const res = await fetch(url, options);
    if (!res.ok) {
        const errorText = await res.text();
        throw new Error(errorText);
    }
    return res.status === 204 ? ({} as T) : res.json();
}

/**
 * Flashes an element with a temporary highlight animation.
 * @param el The element to flash.
 */
export function flashElement(el: HTMLElement): void {
    el.classList.add("flash-change");
    setTimeout(() => el.classList.remove("flash-change"), 1000);
}

/**
 * Finds by Id and removes the d-none class
 * @param el The element ID.
 */
export function showElementById(id: string) {
    const element = document.getElementById(id);
    if (!element) {
        console.warn(`Element with id '${id}' not found.`);
        return;
    }

    element.classList.remove('d-none');
}
