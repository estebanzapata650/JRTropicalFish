import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";

const eslintConfig = defineConfig([
  ...nextVitals,
  ...nextTs,
  // Override default ignores of eslint-config-next.
  globalIgnores([
    // Los patrones por defecto de eslint-config-next solo cubren la raiz. Se usan con **/ para que
    // un .next generado en una subcarpeta (Turbopack lo hace si pierde su directorio raiz) no meta
    // miles de errores de codigo compilado en el lint.
    "**/.next/**",
    "**/out/**",
    "**/build/**",
    "**/next-env.d.ts",
  ]),
]);

export default eslintConfig;
