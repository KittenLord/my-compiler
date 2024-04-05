I'm now realizing that I comically overscoped the set of features, even though, supposedly this language was quite simple. Will probably start a new one, *even simpler*, that I'll actually manage to implement







A tiny compiler I'm doing for educational purposes. It is pretty much C with even less features and hence even less usable.

Grammar, examples and stuff can be viewed in Refs/


Here is the current planned features list:
- Comically simple (read: barebones)
- Block based
- For some reason I made it look like Rust a lot
- Compiled language (will probably use NASM)
- I will indeed probably rethink this one, cuz otherwise the language isn't at all practical ~~Only stack memory allocation, because pointers are too hard (may rethink)~~
- No type stuff besides primitives, arrays and structs (maybe unions as well?)

TODO:
- [ ] Implement all remaining keywords (return, free, for/from/to)
- [ ] Figure out pre-existing functions/constants (new, NULL, NONE, io)
- [ ] Start working on static analysis
- [x] Rewrite the single literal parser (so that chained array dereferences/function indexation is possible)
- [x] Add pointers (@)
