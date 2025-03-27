export type CSNumericType =
  | "Single"
  | "Int32"
  | "Int64"
  | "Double"
  | "Decimal"
  | "Byte"
  | "SByte"
  | "UInt32"
  | "UInt64"
  | "Int16"
  | "UInt16";

/**
 * Enhanced integer parsing with support for specific C# integer types
 * @param value The string to parse
 * @param csType The C# type to enforce
 * @param radix The base (2-36)
 * @returns Number or BigInt value within the valid range for the specified C# type
 * @throws Error if the value is outside the valid range for the specified type
 */
export function parseIntCS(
  value: string,
  csType: CSNumericType,
  radix: number = 10
): number | bigint {
  // For empty strings or non-numeric inputs
  if (!value || !/^[-+]?[0-9a-fA-F]+$/.test(value)) {
    return NaN;
  }

  // Define range limits for C# types
  const ranges = {
    Byte: { min: 0, max: 255 },
    SByte: { min: -128, max: 127 },
    Int16: { min: -32768, max: 32767 },
    UInt16: { min: 0, max: 65535 },
    Int32: { min: -2147483648, max: 2147483647 },
    UInt32: { min: 0, max: 4294967295 },
    Int64: {
      min: BigInt("-9223372036854775808"),
      max: BigInt("9223372036854775807"),
    },
    UInt64: { min: BigInt(0), max: BigInt("18446744073709551615") },
    // Including numeric types for completeness
    Single: { min: -3.4e38, max: 3.4e38 },
    Double: { min: -1.7e308, max: 1.7e308 },
    Decimal: {
      min: BigInt("-79228162514264337593543950335"),
      max: BigInt("79228162514264337593543950335"),
    },
  };

  let result: number | bigint;

  // Use BigInt for large integer types
  if (csType === "Int64" || csType === "UInt64" || csType === "Decimal") {
    try {
      result = BigInt(value);
    } catch {
      return NaN;
    }

    // Check range for BigInt types
    const range = ranges[csType];
    if (result < range.min || result > range.max) {
      throw new Error(`Value out of range for ${csType}: ${value}`);
    }
  } else {
    // Use regular parseInt for smaller integers
    result = parseInt(value, radix);

    // For integer types, check range
    if (
      ["Byte", "SByte", "Int16", "UInt16", "Int32", "UInt32"].includes(csType)
    ) {
      const range = ranges[csType];
      if (result < range.min || result > range.max) {
        throw new Error(`Value out of range for ${csType}: ${value}`);
      }
    }
  }

  return result;
}

/**
 * Enhanced float parsing with support for specific C# floating-point types
 * @param value The string to parse
 * @param csType The C# type to enforce
 * @returns String value within the valid range for the specified C# type,
 *          with formatting rules applied
 * @throws Error if the value is outside the valid range for the specified type
 */
export function parseFloatCS(value: string, csType: CSNumericType): string {
  // For empty strings or non-numeric inputs
  if (!value || !/^[-+]?[0-9]*\.?[0-9]*([eE][-+]?[0-9]+)?$/.test(value)) {
    return "NaN";
  }

  // Define range limits for C# types
  const ranges = {
    Single: { min: -3.4e38, max: 3.4e38, precision: 7 },
    Double: { min: -1.7e308, max: 1.7e308, precision: 15 },
    Decimal: {
      min: Number("-7.9e28"),
      max: Number("7.9e28"),
      precision: 28,
    },
    // Including integer types for completeness
    Byte: { min: 0, max: 255 },
    SByte: { min: -128, max: 127 },
    Int16: { min: -32768, max: 32767 },
    UInt16: { min: 0, max: 65535 },
    Int32: { min: -2147483648, max: 2147483647 },
    UInt32: { min: 0, max: 4294967295 },
    Int64: { min: Number.MIN_SAFE_INTEGER, max: Number.MAX_SAFE_INTEGER }, // Simplified for string check
    UInt64: { min: 0, max: Number.MAX_SAFE_INTEGER }, // Simplified for string check
  };

  // Check if the number is within range by attempting to parse it
  const numericValue = parseFloat(value);

  // Check for NaN and infinity
  if (isNaN(numericValue) || !isFinite(numericValue)) {
    throw new Error(`Invalid numeric value for ${csType}: ${value}`);
  }

  // Special range checking for big integer types
  if (csType === "Int64" || csType === "UInt64" || csType === "Decimal") {
    // If no decimal point, check if it's a valid integer within range
    if (!value.includes(".") && !value.includes("e") && !value.includes("E")) {
      try {
        const bigValue = BigInt(value);

        // Range check based on C# type
        if (
          csType === "Int64" &&
          (bigValue < BigInt("-9223372036854775808") ||
            bigValue > BigInt("9223372036854775807"))
        ) {
          throw new Error(`Value out of range for Int64: ${value}`);
        }

        if (
          csType === "UInt64" &&
          (bigValue < BigInt(0) || bigValue > BigInt("18446744073709551615"))
        ) {
          throw new Error(`Value out of range for UInt64: ${value}`);
        }

        if (
          csType === "Decimal" &&
          (bigValue < BigInt("-79228162514264337593543950335") ||
            bigValue > BigInt("79228162514264337593543950335"))
        ) {
          throw new Error(`Value out of range for Decimal: ${value}`);
        }
      } catch (e) {
        if (String(e).includes("out of range")) {
          throw e;
        }
        // If it's not a valid BigInt, continue with regular checks
      }
    } else if (csType === "Decimal") {
      // For Decimal with floating point, check scale and precision
      // Simplified here - a real implementation would need more validation
      if (Math.abs(numericValue) > 7.9e28) {
        throw new Error(`Value out of range for Decimal: ${value}`);
      }

      // Check digits not counting leading/trailing zeros and decimal point
      const significantDigits = value
        .replace(/^[-+]/, "") // Remove sign
        .replace(/^0+/, "") // Remove leading zeros
        .replace(/\./, "") // Remove decimal point
        .replace(/[eE][-+]?[0-9]+$/, "") // Remove exponent
        .replace(/0+$/, "").length; // Remove trailing zeros

      if (significantDigits > 29) {
        throw new Error(
          `Precision exceeds Decimal's 28-29 significant digits: ${value}`
        );
      }
    }
  } else {
    // For regular numeric types
    const range = ranges[csType];
    if (numericValue < range.min || numericValue > range.max) {
      throw new Error(`Value out of range for ${csType}: ${value}`);
    }

    // For integer types, verify it's an integer
    if (
      ["Byte", "SByte", "Int16", "UInt16", "Int32", "UInt32"].includes(csType)
    ) {
      if (!Number.isInteger(numericValue)) {
        throw new Error(`Value must be an integer for ${csType}: ${value}`);
      }
    }
  }

  // Format the output value according to requirements
  let formattedValue = value;

  // Preserve the trailing decimal point if it exists in the original input
  const hasTrailingDecimal = value.endsWith(".");

  // Handle leading zeros according to requirements
  const isNegative = value.startsWith("-");
  let withoutSign = isNegative ? value.substring(1) : value;

  // Check if we have leading zeros to process
  if (/^0[0-9]/.test(withoutSign)) {
    if (withoutSign.includes(".")) {
      // Case for decimal numbers with leading zeros
      if (withoutSign.startsWith("0.")) {
        // Special case: 0.x should remain as 0.x
        // Do nothing, keep as is
      } else {
        // Cases like 02.4 -> 2.4 or 00.1 -> 0.1
        withoutSign = withoutSign.replace(/^0+(?=\d)/, "");

        // Special case: ensure at least one 0 before decimal point if needed
        if (withoutSign.startsWith(".")) {
          withoutSign = "0" + withoutSign;
        }
      }
    } else {
      // For integers with leading zeros: 03 -> 3
      withoutSign = withoutSign.replace(/^0+/, "");
      if (withoutSign === "") withoutSign = "0"; // Handle case of all zeros
    }

    // Reapply sign if needed
    formattedValue = isNegative ? "-" + withoutSign : withoutSign;
  }

  // Ensure trailing decimal is preserved
  if (hasTrailingDecimal && !formattedValue.endsWith(".")) {
    formattedValue = formattedValue + ".";
  }

  return formattedValue;
}
