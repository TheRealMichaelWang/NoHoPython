# The North-Hollywood Python Compiler
A strongly-typed, memory-safe, compiled dialect of Python, that transpiles to human-readable C.
- Guarentees memory saftey by enforcing RAII, and executing limited reference counting for closure-captured classes
- Guarentees type saftey via a strong yet flexible static type-checker
  - No null. Enums/Variants are used instead. 
  - Enum/Variant match statements must be exhaustive.
- Supports first-class, anonymous functions
  - Functions can be passed around as values
  - Supports closures, which are functions that can capture variables outside of it's lexical scope
  - No globals. Functions only can mutate arguments, and captured classes in addition to it's locally defined varaibles.
- Code organized via a powerful module system
  - No C++-esque `using namespace`'s(prevents pollution of the global namespace)
- Runtime Saftey
  - Array access' are all bounds checked.
- Easy C interop
  - Effortless FFI interop with C, via the `cdef` keyword. 
  - Include C headers with the `cinclude` keyword
- Information-Rich Error-Reporting
  - Clean, precise error messages during compile time
  - Runtime errors, such as assertion faliures or index-out-of-bounds errors, are reported with their locations in the source code.
