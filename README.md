A tiny compiler I'm doing for educational purposes. It is pretty much C with even less features and hence even less usable.

Grammar, examples and stuff can be viewed in Refs/


Here is the current planned features list:
- Comically simple (read: barebones)
- Block based
- For some reason I made it look like Rust a lot
- Compiled language (will probably use NASM)
- No LLVM, custom IR and optimizations
- Only stack memory allocation, because pointers are too hard (may rethink)
- No type stuff besides primitives, arrays and structs (maybe unions as well?)
