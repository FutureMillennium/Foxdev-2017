@call vars.cmd

@SET "PATH=%qemu%;%PATH%"

if defined %1 then (
qemu-system-i386 -kernel %1
) else (qemu-system-i386 -fda boot1.bin)
