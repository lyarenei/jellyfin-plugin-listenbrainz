import eslint from "@eslint/js";
import prettier from "eslint-config-prettier";
import tseslint from "typescript-eslint";

export default tseslint.config(
    {
        ignores: ["**/node_modules/", "**/bin/", "**/obj/", "types/"],
    },
    {
        files: ["src/**/*.ts"],
        extends: [eslint.configs.recommended, ...tseslint.configs.recommended, prettier],
        rules: {
            "no-unused-vars": "off",
            "@typescript-eslint/no-unused-vars": ["error", { argsIgnorePattern: "^_", varsIgnorePattern: "^_" }],
            "no-restricted-globals": [
                "error",
                {
                    name: "ApiClient",
                    message: "Use local wrapper defined in apiClient.ts instead of the global ApiClient.",
                },
            ],
        },
    },
    {
        files: ["**/apiClient.ts"],
        rules: {
            "no-restricted-globals": "off",
        },
    },
);
