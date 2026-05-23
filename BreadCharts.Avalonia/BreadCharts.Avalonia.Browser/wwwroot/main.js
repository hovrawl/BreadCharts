import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = dotnetRuntime.getConfig();

console.log("Loading exports for assembly:", "BreadCharts.Avalonia");
const runtimeExports = await dotnetRuntime.getAssemblyExports("BreadCharts.Avalonia");
console.log("Runtime exports:", runtimeExports);
export const authService = runtimeExports.BreadCharts.Avalonia.Services.AuthService;

if (authService && authService.Log) {
    authService.Log("AuthService exports loaded successfully");
} else {
    console.error("Failed to load AuthService exports", runtimeExports);
}

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);

export function openUrl(url) {
    window.location.href = url;
}

export function openPopup(url) {
    console.log("Opening auth popup with URL:", url);
    const width = 500;
    const height = 700;
    const left = (window.screen.width / 2) - (width / 2);
    const top = (window.screen.height / 2) - (height / 2);
    
    const popup = window.open(url, 'Spotify Login', `width=${width},height=${height},top=${top},left=${left}`);
    
    if (window.focus && popup) {
        popup.focus();
    } else if (!popup) {
        console.error("Popup was blocked by browser");
        authService.Log("Popup was blocked by browser. Please allow popups for this site.");
        return;
    }

    const messageListener = (event) => {
        console.log("Received message from popup:", event.data);
        if (event.source === popup) {
            console.log("Message source matches popup");
            if (typeof event.data === 'string' && event.data.includes('auth_callback.html')) {
                console.log("Auth callback URL detected, notifying .NET");
                // Call back into .NET
                authService.OnAuthCompleted(event.data);
                popup.close();
                window.removeEventListener('message', messageListener);
            }
        } else {
            console.warn("Received message from unknown source", event.source);
        }
    };
    window.addEventListener('message', messageListener);
}
