"use client";

import { useMsal, useIsAuthenticated } from "@azure/msal-react";

export const UserProfile = () => {
    const { instance, accounts } = useMsal();
    const isAuthenticated = useIsAuthenticated();
    const name = accounts[0] && accounts[0].name;

    const handleLogout = () => {
        instance.logoutRedirect().catch((e: Error) => {
            console.error(e);
        });
    };

    return isAuthenticated ? (
        <div className="flex items-center gap-4" data-testid="user-profile">
            <p data-testid="user-email">Welcome, {name}</p>
            <button
                onClick={handleLogout}
                className="bg-red-500 hover:bg-red-700 text-white font-bold py-2 px-4 rounded"
            >
                Logout
            </button>
        </div>
    ) : null;
};