// Copyright (C) 2018-2021, The Replanetizer Contributors.
// Replanetizer is free software: you can redistribute it
// and/or modify it under the terms of the GNU General Public
// License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// Please see the LICENSE.md file for more details.

using System.ComponentModel;
using LibReplanetizer.Models.Animations;
using System.Collections.Generic;
using System.IO;
using static LibReplanetizer.DataFunctions;

namespace LibReplanetizer.Models
{
    public class MobyModel : Model
    {
        private static readonly NLog.Logger LOGGER = NLog.LogManager.GetCurrentClassLogger();

        const int TEXTUREDVERTELEMENTSIZE = 0x28;
        const int REFLECTIVEVERTELEMENTSIZE = 0x20;
        const int SUBMESHELEMENTSIZE = 0x10;
        const int MESHHEADERSIZE = 0x20;
        const int HEADERSIZE = 0x48;

        public int null1 { get; set; }

        [Category("Attributes"), DisplayName("Bone Count")]
        public byte boneCount { get; set; }
        [Category("Attributes"), DisplayName("Low Poly Bone Count")]
        public byte lpBoneCount { get; set; }            // Low poly bone count
        public byte count3 { get; set; }
        public byte count4 { get; set; }
        [Category("Attributes"), DisplayName("Low Poly Render Distance")]
        public byte lpRenderDist { get; set; }            // Low poly render distance
        public byte count8 { get; set; }

        public int null2 { get; set; }
        public int null3 { get; set; }

        /*
         * These are not zero, yet setting them to 0 has no effect on Oozla (maybe test other levels?)
         */
        public float unk1 { get; set; }
        public float unk2 { get; set; }
        public float unk3 { get; set; }
        public float unk4 { get; set; }

        public uint color2 { get; set; }               // RGBA color
        public uint unk6 { get; set; }

        public ushort litVertexCount { get; set; }

        [Category("Attributes"), DisplayName("Animations")]
        public List<Animation> animations { get; set; } = new List<Animation>();
        [Category("Attributes"), DisplayName("Sounds")]
        public List<ModelSound> modelSounds { get; set; } = new List<ModelSound>();
        [Category("Attributes"), DisplayName("Attachments")]
        public List<Attachment> attachments { get; set; } = new List<Attachment>();
        [Category("Attributes"), DisplayName("Index Attachments")]
        public List<byte> indexAttachments { get; set; } = new List<byte>();
        [Category("Attributes"), DisplayName("Bone Matrices")]
        public List<BoneMatrix> boneMatrices { get; set; } = new List<BoneMatrix>();
        [Category("Attributes"), DisplayName("Bone Datas")]
        public List<BoneData> boneDatas { get; set; } = new List<BoneData>();

        [Category("Unknowns"), DisplayName("Reflective Vertex Buffer")]
        public float[] reflectiveVertexBuffer { get; set; } = new float[0];

        [Category("Unknowns"), DisplayName("Reflective Texture Configurations")]
        public List<TextureConfig> reflectiveTextureConfigs { get; set; } = new List<TextureConfig>();

        [Category("Unknowns"), DisplayName("Reflective Index Buffer")]
        public ushort[] reflectiveIndexBuffer { get; set; } = new ushort[0];
        public Skeleton? skeleton = null;
        [Category("Attributes"), DisplayName("Is Model")]
        public bool isModel { get; set; } = true;


        // Unparsed sections
        public byte[] type10Block = { };                  // Hitbox


        public MobyModel() { }

        public MobyModel(FileStream fs, GameType game, short modelID, int offset)
        {
            id = modelID;
            if (offset == 0x00)
            {
                isModel = false;
                return;
            }

            // Header
            byte[] headBlock = ReadBlock(fs, offset, HEADERSIZE);

            int meshPointer = ReadInt(headBlock, 0x00);
            null1 = ReadInt(headBlock, 0x04);

            boneCount = headBlock[0x08];
            lpBoneCount = headBlock[0x09];

            if (boneCount == 0) boneCount = lpBoneCount;

            count3 = headBlock[0x0A];
            count4 = headBlock[0x0B];
            byte animationCount = headBlock[0x0C];
            byte soundCount = headBlock[0x0D];
            lpRenderDist = headBlock[0x0E];
            count8 = headBlock[0x0F];

            int type10Pointer = ReadInt(headBlock, 0x10);
            int boneMatrixPointer = ReadInt(headBlock, 0x14);
            int boneDataPointer = ReadInt(headBlock, 0x18);
            int attachmentPointer = ReadInt(headBlock, 0x1C);

            null2 = ReadInt(headBlock, 0x20);
            size = ReadFloat(headBlock, 0x24);
            int soundPointer = ReadInt(headBlock, 0x28);
            null3 = ReadInt(headBlock, 0x2C);

            if (null1 != 0 || null2 != 0 || null3 != 0) { LOGGER.Warn("Warning: null in model header wan't null"); }

            unk1 = ReadFloat(headBlock, 0x30);
            unk2 = ReadFloat(headBlock, 0x34);
            unk3 = ReadFloat(headBlock, 0x38);
            unk4 = ReadFloat(headBlock, 0x3C);

            color2 = ReadUint(headBlock, 0x40);
            unk6 = ReadUint(headBlock, 0x44);

            // Animation block
            byte[] animationPointerBlock = ReadBlock(fs, offset + HEADERSIZE, animationCount * 0x04);

            for (int i = 0; i < animationCount; i++)
            {
                animations.Add(new Animation(fs, game, offset, ReadInt(animationPointerBlock, i * 0x04), boneCount));
            }

            // Type 10 ( has something to do with collision )
            if (type10Pointer > 0)
            {
                byte[] type10Head = ReadBlock(fs, offset + type10Pointer, 0x10);
                int type10Length = ReadInt(type10Head, 0x04) + ReadInt(type10Head, 0x08) + ReadInt(type10Head, 0x0C);
                type10Block = ReadBlock(fs, offset + type10Pointer, 0x10 + type10Length);
            }

            // Bone matrix

            if (boneMatrixPointer > 0)
            {
                byte[] boneMatrixBlock = ReadBlock(fs, offset + boneMatrixPointer, boneCount * 0x40);
                for (int i = 0; i < boneCount; i++)
                {
                    boneMatrices.Add(new BoneMatrix(game, boneMatrixBlock, i));
                }
            }


            // Bone data

            if (boneDataPointer > 0)
            {
                byte[] boneDataBlock = ReadBlock(fs, offset + boneDataPointer, boneCount * 0x10);
                for (int i = 0; i < boneCount; i++)
                {
                    boneDatas.Add(new BoneData(game, boneDataBlock, i));
                }
            }



            // Attachments
            if (attachmentPointer > 0)
            {
                int attachmentCount = ReadInt(ReadBlock(fs, offset + attachmentPointer, 4), 0);
                if (attachmentCount > 0)
                {
                    byte[] headerBlock = ReadBlock(fs, offset + attachmentPointer + 4, attachmentCount * 4);
                    for (int i = 0; i < attachmentCount; i++)
                    {
                        int attachmentOffset = ReadInt(headerBlock, i * 4);
                        attachments.Add(new Attachment(fs, offset + attachmentOffset));
                    }
                }
                else
                {
                    int attid = 0;
                    while (true)
                    {
                        byte val = ReadBlock(fs, offset + attachmentPointer + 4 + attid, 1)[0];
                        if (val == 0xff) break;
                        indexAttachments.Add(val);
                        attid++;
                    }
                }

            }


            // Sounds
            if (soundPointer > 0)
            {
                byte[] soundBlock = ReadBlock(fs, offset + soundPointer, soundCount * 0x20);
                for (int i = 0; i < soundCount; i++)
                {
                    modelSounds.Add(new ModelSound(soundBlock, i));
                }
            }

            // Mesh meta
            if (meshPointer > 0)
            {
                byte[] meshHeader = ReadBlock(fs, offset + meshPointer, 0x20);

                int texturedSubMeshCount = ReadInt(meshHeader, 0x00);
                int reflectiveSubMeshCount = ReadInt(meshHeader, 0x04);
                int texturedSubMeshPointer = offset + ReadInt(meshHeader, 0x08);
                int reflectiveSubMeshPointer = offset + ReadInt(meshHeader, 0x0C);
                int vertPointer = offset + ReadInt(meshHeader, 0x10);
                int indexPointer = offset + ReadInt(meshHeader, 0x14);
                ushort texturedVertexCount = ReadUshort(meshHeader, 0x18);
                ushort reflectiveVertexCount = ReadUshort(meshHeader, 0x18);

                int reflectiveVertPointer = vertPointer + texturedVertexCount * 0x28;

                litVertexCount = ReadUshort(meshHeader, 0x1C);     //These vertices are not affected by color2

                int faceCount = 0;

                //Texture configuration
                if (texturedSubMeshPointer > 0)
                {
                    mappedTextureConfigs = GetTextureConfigs(fs, texturedSubMeshPointer, texturedSubMeshCount, SUBMESHELEMENTSIZE);
                    faceCount = GetFaceCount();
                }

                if (vertPointer > 0 && texturedVertexCount > 0)
                {
                    //Get vertex buffer float[vertX, vertY, vertZ, normX, normY, normZ, U, V, reserved, reserved]
                    vertexBuffer = GetVertices(fs, vertPointer, texturedVertexCount, TEXTUREDVERTELEMENTSIZE);
                }

                if (indexPointer > 0 && faceCount > 0)
                {
                    //Index buffer
                    indexBuffer = GetIndices(fs, indexPointer, faceCount);
                }
                if (reflectiveSubMeshPointer > 0)
                {
                    reflectiveVertexBuffer = GetReflectiveVertices(fs, reflectiveVertPointer, reflectiveVertexCount, REFLECTIVEVERTELEMENTSIZE);
                    reflectiveTextureConfigs = GetTextureConfigs(fs, reflectiveSubMeshPointer, reflectiveSubMeshCount, SUBMESHELEMENTSIZE);
                    int reflectiveFaceCount = 0;
                    foreach (TextureConfig tex in reflectiveTextureConfigs)
                    {
                        reflectiveFaceCount += tex.size;
                    }
                    reflectiveIndexBuffer = GetIndices(fs, indexPointer + faceCount * sizeof(ushort), reflectiveFaceCount);
                }
            }

            if (boneMatrices.Count > 0 && boneDatas.Count > 0)
            {
                skeleton = new Skeleton(boneMatrices[0], null);

                for (int i = 1; i < boneCount; i++)
                {
                    skeleton.InsertBone(boneMatrices[i], boneDatas[i].parent);
                }
            }
        }

        /*
         * RaC 2 and 3 armor files contain only the mesh
         */
        public static MobyModel GetArmorMobyModel(FileStream fileStream, int modelPointer)
        {
            MobyModel model = new MobyModel();

            model.size = 1.0f;

            byte[] meshHeader = ReadBlock(fileStream, modelPointer, 0x20);

            int texturedSubMeshCount = ReadInt(meshHeader, 0x00);
            int reflectiveSubMeshCount = ReadInt(meshHeader, 0x04);
            int texturedSubMeshPointer = ReadInt(meshHeader, 0x08);
            int reflectiveSubMeshPointer = ReadInt(meshHeader, 0x0C);
            int vertPointer = ReadInt(meshHeader, 0x10);
            int indexPointer = ReadInt(meshHeader, 0x14);
            ushort texturedVertexCount = ReadUshort(meshHeader, 0x18);
            ushort reflectiveVertexCount = ReadUshort(meshHeader, 0x18);

            int reflectiveVertPointer = vertPointer + texturedVertexCount * 0x28;

            model.litVertexCount = ReadUshort(meshHeader, 0x1C);     //These vertices are not affected by color2

            int faceCount = 0;

            //Texture configuration
            if (texturedSubMeshPointer > 0)
            {
                model.mappedTextureConfigs = GetTextureConfigs(fileStream, texturedSubMeshPointer, texturedSubMeshCount, SUBMESHELEMENTSIZE);
                faceCount = model.GetFaceCount();
            }

            if (vertPointer > 0 && texturedVertexCount > 0)
            {
                //Get vertex buffer float[vertX, vertY, vertZ, normX, normY, normZ, U, V, reserved, reserved]
                model.vertexBuffer = model.GetVertices(fileStream, vertPointer, texturedVertexCount, TEXTUREDVERTELEMENTSIZE);
            }

            if (indexPointer > 0 && faceCount > 0)
            {
                //Index buffer
                model.indexBuffer = GetIndices(fileStream, indexPointer, faceCount);
            }
            if (reflectiveSubMeshPointer > 0)
            {
                model.reflectiveVertexBuffer = model.GetReflectiveVertices(fileStream, reflectiveVertPointer, reflectiveVertexCount, REFLECTIVEVERTELEMENTSIZE);
                model.reflectiveTextureConfigs = GetTextureConfigs(fileStream, reflectiveSubMeshPointer, reflectiveSubMeshCount, SUBMESHELEMENTSIZE);
                int otherfaceCount = 0;
                foreach (TextureConfig tex in model.reflectiveTextureConfigs)
                {
                    otherfaceCount += tex.size;
                }
                model.reflectiveIndexBuffer = GetIndices(fileStream, indexPointer + faceCount * sizeof(ushort), otherfaceCount);
            }

            return model;
        }

        /*
         * RaC 2 and 3 gadgets files contain only the mesh
         * Same Format is also used for DL missions
         */
        public static MobyModel GetGadgetMobyModel(FileStream fileStream, int modelPointer)
        {
            MobyModel model = new MobyModel();

            int modelHeadSize = ReadInt(ReadBlock(fileStream, modelPointer, 0x04), 0x00);

            if (modelHeadSize == 0) return model;

            byte[] meshHeader = ReadBlock(fileStream, modelPointer, modelHeadSize + 0x20);

            int objectPointer = ReadInt(meshHeader, 0x00);

            int texturedSubMeshCount = ReadInt(meshHeader, objectPointer + 0x00);
            int reflectiveSubMeshCount = ReadInt(meshHeader, objectPointer + 0x04);
            int texturedSubMeshPointer = ReadInt(meshHeader, objectPointer + 0x08);
            int reflectiveSubMeshPointer = ReadInt(meshHeader, objectPointer + 0x0C);
            int vertPointer = ReadInt(meshHeader, objectPointer + 0x10);
            int indexPointer = ReadInt(meshHeader, objectPointer + 0x14);
            ushort texturedVertexCount = ReadUshort(meshHeader, objectPointer + 0x18);
            ushort reflectiveVertexCount = ReadUshort(meshHeader, objectPointer + 0x18);

            int reflectiveVertPointer = vertPointer + texturedVertexCount * 0x28;

            model.litVertexCount = ReadUshort(meshHeader, 0x1C);     //These vertices are not affected by color2

            int faceCount = 0;

            //Texture configuration
            if (texturedSubMeshPointer > 0)
            {
                model.mappedTextureConfigs = GetTextureConfigs(fileStream, texturedSubMeshPointer, texturedSubMeshCount, SUBMESHELEMENTSIZE);
                faceCount = model.GetFaceCount();
            }

            if (vertPointer > 0 && texturedVertexCount > 0)
            {
                //Get vertex buffer float[vertX, vertY, vertZ, normX, normY, normZ, U, V, reserved, reserved]
                model.vertexBuffer = model.GetVertices(fileStream, vertPointer, texturedVertexCount, TEXTUREDVERTELEMENTSIZE);
            }

            if (indexPointer > 0 && faceCount > 0)
            {
                //Index buffer
                model.indexBuffer = GetIndices(fileStream, indexPointer, faceCount);
            }
            if (reflectiveSubMeshPointer > 0)
            {
                model.reflectiveVertexBuffer = model.GetReflectiveVertices(fileStream, reflectiveVertPointer, reflectiveVertexCount, REFLECTIVEVERTELEMENTSIZE);
                model.reflectiveTextureConfigs = GetTextureConfigs(fileStream, reflectiveSubMeshPointer, reflectiveSubMeshCount, SUBMESHELEMENTSIZE);
                int otherfaceCount = 0;
                foreach (TextureConfig tex in model.reflectiveTextureConfigs)
                {
                    otherfaceCount += tex.size;
                }
                model.reflectiveIndexBuffer = GetIndices(fileStream, indexPointer + faceCount * sizeof(ushort), otherfaceCount);
            }

            return model;
        }

        public byte[] SerializeReflectiveVertices()
        {
            int elemSize = 0x20;
            byte[] outBytes = new byte[(vertexBuffer.Length / 8) * elemSize];

            for (int i = 0; i < vertexBuffer.Length / 8; i++)
            {
                WriteFloat(outBytes, (i * elemSize) + 0x00, vertexBuffer[(i * 6) + 0]);
                WriteFloat(outBytes, (i * elemSize) + 0x04, vertexBuffer[(i * 6) + 1]);
                WriteFloat(outBytes, (i * elemSize) + 0x08, vertexBuffer[(i * 6) + 2]);
                WriteFloat(outBytes, (i * elemSize) + 0x0C, vertexBuffer[(i * 6) + 3]);
                WriteFloat(outBytes, (i * elemSize) + 0x10, vertexBuffer[(i * 6) + 4]);
                WriteFloat(outBytes, (i * elemSize) + 0x14, vertexBuffer[(i * 6) + 5]);
                WriteUint(outBytes, (i * elemSize) + 0x18, weights[i]);
                WriteUint(outBytes, (i * elemSize) + 0x1C, ids[i]);
            }

            return outBytes;
        }

        public byte[] GetReflectiveFaceBytes(ushort offset = 0)
        {
            byte[] indexBytes = new byte[reflectiveIndexBuffer.Length * sizeof(ushort)];
            for (int i = 0; i < reflectiveIndexBuffer.Length; i++)
            {
                WriteUshort(indexBytes, i * sizeof(ushort), (ushort) (reflectiveIndexBuffer[i] + offset));
            }
            return indexBytes;
        }


        public byte[] Serialize(int offset)
        {
            // Sometimes the mobys offset is not 0x10 aligned with the file,
            // but the internal offsets are supposed to be
            int alignment = 0x10 - (offset % 0x10);
            if (alignment == 0x10) alignment = 0;

            // We need to reserve some room for Ratchet's menu animations
            // this is hardcoded as 0x1c in the ELF, thus we have to just check
            // if the id of the current model is 0 I.E Ratchet, and add this offset
            int stupidOffset = 0;
            if (id == 0)
            {
                stupidOffset = 0x20 * 4;
            }



            byte[] vertexBytes = SerializeVertices();
            byte[] faceBytes = GetFaceBytes();


            byte[] reflectiveVertexBytes = SerializeReflectiveVertices();
            byte[] reflectiveFaceBytes = GetReflectiveFaceBytes();

            //sounds
            byte[] soundBytes = new byte[modelSounds.Count * 0x20];
            for (int i = 0; i < modelSounds.Count; i++)
            {
                byte[] soundByte = modelSounds[i].Serialize();
                soundByte.CopyTo(soundBytes, i * 0x20);
            }

            //boneMatrix
            byte[] boneMatrixBytes = new byte[boneMatrices.Count * 0x40];
            for (int i = 0; i < boneMatrices.Count; i++)
            {
                byte[] boneMatrixByte = boneMatrices[i].Serialize();
                boneMatrixByte.CopyTo(boneMatrixBytes, i * 0x40);
            }

            //boneData
            byte[] boneDataBytes = new byte[boneDatas.Count * 0x10];
            for (int i = 0; i < boneDatas.Count; i++)
            {
                byte[] boneDataByte = boneDatas[i].Serialize();
                boneDataByte.CopyTo(boneDataBytes, i * 0x10);
            }

            int hack = 0;
            if (id > 2) hack = 0x20;
            int meshDataOffset = GetLength(HEADERSIZE + animations.Count * 4 + stupidOffset + hack, alignment);
            int textureConfigOffset = GetLength(meshDataOffset + 0x20, alignment);
            int otherTextureConfigOffset = GetLength(textureConfigOffset + mappedTextureConfigs.Count * 0x10, alignment);

            int file80 = 0;
            if (vertexBuffer.Length != 0)
                file80 = DistToFile80(offset + otherTextureConfigOffset + reflectiveTextureConfigs.Count * 0x10);
            int vertOffset = GetLength(otherTextureConfigOffset + reflectiveTextureConfigs.Count * 0x10 + file80, alignment);
            int reflectiveVertOffset = vertOffset + vertexBytes.Length;
            int faceOffset = GetLength(reflectiveVertOffset + reflectiveVertexBytes.Length, alignment);
            int reflectiveFaceOffset = faceOffset + faceBytes.Length;
            int type10Offset = GetLength(reflectiveFaceOffset + reflectiveFaceBytes.Length, alignment);
            int soundOffset = GetLength(type10Offset + type10Block.Length, alignment);
            int attachmentOffset = GetLength(soundOffset + soundBytes.Length, alignment);


            List<byte> attachmentBytes = new List<byte>();
            if (attachments.Count > 0)
            {
                byte[] attachmentHead = new byte[4 + attachments.Count * 4];
                WriteInt(attachmentHead, 0, attachments.Count);
                int attOffset = attachmentOffset + 4 + attachments.Count * 4;
                for (int i = 0; i < attachments.Count; i++)
                {
                    WriteInt(attachmentHead, 4 + i * 4, attOffset);
                    byte[] attBytes = attachments[i].Serialize();
                    attachmentBytes.AddRange(attBytes);
                    attOffset += attBytes.Length;
                }
                attachmentBytes.InsertRange(0, attachmentHead);
            }
            else if (indexAttachments.Count > 0)
            {
                attachmentBytes.AddRange(new byte[] { 0, 0, 0, 0 });
                attachmentBytes.AddRange(indexAttachments);
                attachmentBytes.Add(0xff);
            }



            int boneMatrixOffset = GetLength(attachmentOffset + attachmentBytes.Count, alignment);
            int boneDataOffset = GetLength(boneMatrixOffset + boneMatrixBytes.Length, alignment);
            int animationOffset = GetLength(boneDataOffset + boneDataBytes.Length, alignment);
            int newAnimationOffset = animationOffset;
            List<byte> animByteList = new List<byte>();

            List<int> animOffsets = new List<int>();

            foreach (Animation anim in animations)
            {
                if (anim.frames.Count != 0)
                {
                    animOffsets.Add(newAnimationOffset);
                    byte[] anima = anim.Serialize(newAnimationOffset, offset);
                    animByteList.AddRange(anima);
                    newAnimationOffset += anima.Length;
                }
                else
                {
                    animOffsets.Add(0);
                }
            }

            int modelLength = newAnimationOffset;
            byte[] outbytes = new byte[modelLength];


            // Header
            if (vertexBuffer.Length != 0)
                WriteInt(outbytes, 0x00, meshDataOffset);

            outbytes[0x08] = boneCount;
            outbytes[0x09] = lpBoneCount;
            outbytes[0x0A] = count3;
            outbytes[0x0B] = count4;
            outbytes[0x0C] = (byte) animations.Count;
            outbytes[0x0D] = (byte) modelSounds.Count;
            outbytes[0x0E] = lpRenderDist;
            outbytes[0x0F] = count8;

            if (type10Block.Length != 0)
                WriteInt(outbytes, 0x10, type10Offset);

            if (id != 1 && id != 2)
            {
                WriteInt(outbytes, 0x14, boneMatrixOffset);
                WriteInt(outbytes, 0x18, boneDataOffset);
            }

            if (attachments.Count != 0 || indexAttachments.Count != 0)
                WriteInt(outbytes, 0x1C, attachmentOffset);


            //null
            WriteFloat(outbytes, 0x24, size);
            if (modelSounds.Count != 0)
                WriteInt(outbytes, 0x28, soundOffset);


            //null

            WriteFloat(outbytes, 0x30, unk1);
            WriteFloat(outbytes, 0x34, unk2);
            WriteFloat(outbytes, 0x38, unk3);
            WriteFloat(outbytes, 0x3C, unk4);

            WriteUint(outbytes, 0x40, color2);
            WriteUint(outbytes, 0x44, unk6);

            for (int i = 0; i < animations.Count; i++)
            {
                WriteInt(outbytes, HEADERSIZE + i * 0x04, animOffsets[i]);
            }

            vertexBytes.CopyTo(outbytes, vertOffset);
            reflectiveVertexBytes.CopyTo(outbytes, reflectiveVertOffset);
            faceBytes.CopyTo(outbytes, faceOffset);
            reflectiveFaceBytes.CopyTo(outbytes, reflectiveFaceOffset);

            if (type10Block != null)
            {
                type10Block.CopyTo(outbytes, type10Offset);
            }

            soundBytes.CopyTo(outbytes, soundOffset);
            attachmentBytes.CopyTo(outbytes, attachmentOffset);
            boneMatrixBytes.CopyTo(outbytes, boneMatrixOffset);
            boneDataBytes.CopyTo(outbytes, boneDataOffset);
            animByteList.CopyTo(outbytes, animationOffset);


            // Mesh header
            WriteInt(outbytes, meshDataOffset + 0x00, mappedTextureConfigs.Count);
            WriteInt(outbytes, meshDataOffset + 0x04, reflectiveTextureConfigs.Count);
            //Othercount
            if (mappedTextureConfigs.Count != 0)
                WriteInt(outbytes, meshDataOffset + 0x08, textureConfigOffset);
            if (reflectiveTextureConfigs.Count != 0)
                WriteInt(outbytes, meshDataOffset + 0x0c, otherTextureConfigOffset);
            //otheroffset
            if (vertexBuffer.Length != 0)
                WriteInt(outbytes, meshDataOffset + 0x10, vertOffset);

            if (faceBytes.Length != 0)
                WriteInt(outbytes, meshDataOffset + 0x14, faceOffset);
            WriteShort(outbytes, meshDataOffset + 0x18, (short) (vertexBytes.Length / TEXTUREDVERTELEMENTSIZE));
            WriteShort(outbytes, meshDataOffset + 0x1a, (short) (reflectiveVertexBytes.Length / REFLECTIVEVERTELEMENTSIZE));
            WriteShort(outbytes, meshDataOffset + 0x1C, (short) (litVertexCount));


            for (int i = 0; i < mappedTextureConfigs.Count; i++)
            {
                WriteInt(outbytes, textureConfigOffset + i * 0x10 + 0x00, mappedTextureConfigs[i].id);
                WriteInt(outbytes, textureConfigOffset + i * 0x10 + 0x04, mappedTextureConfigs[i].start);
                WriteInt(outbytes, textureConfigOffset + i * 0x10 + 0x08, mappedTextureConfigs[i].size);
                WriteInt(outbytes, textureConfigOffset + i * 0x10 + 0x0C, mappedTextureConfigs[i].mode);
            }

            for (int i = 0; i < reflectiveTextureConfigs.Count; i++)
            {
                WriteInt(outbytes, otherTextureConfigOffset + i * 0x10 + 0x00, reflectiveTextureConfigs[i].id);
                WriteInt(outbytes, otherTextureConfigOffset + i * 0x10 + 0x04, reflectiveTextureConfigs[i].start);
                WriteInt(outbytes, otherTextureConfigOffset + i * 0x10 + 0x08, reflectiveTextureConfigs[i].size);
                WriteInt(outbytes, otherTextureConfigOffset + i * 0x10 + 0x0C, reflectiveTextureConfigs[i].mode);
            }

            return outbytes;
        }
    }
}
