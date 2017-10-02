@call vars.cmd

@SET "PATH=%qemu%;%PATH%"

@REM grub-mkrescue -o iso/alopos.iso bin
@REM qemu-system-i386 -cdrom iso/alopos.iso

qemu-system-i386 -kernel hellokernel.bin
